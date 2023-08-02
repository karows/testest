﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using API.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace API.Services;
#nullable enable


public interface ILocalizationService
{
    Task<Dictionary<string, string>?> LoadLanguage(string languageCode);
    Task<string> Get(string locale, string key, params object[] args);
    IEnumerable<string> GetLocales();
}

public class LocalizationService : ILocalizationService
{
    private readonly IDirectoryService _directoryService;
    private readonly IMemoryCache _cache;
    /// <summary>
    /// The locales for the UI
    /// </summary>
    private readonly string _localizationDirectoryUI;

    private readonly MemoryCacheEntryOptions _cacheOptions;


    public LocalizationService(IDirectoryService directoryService, IHostEnvironment environment, IMemoryCache cache)
    {
        _directoryService = directoryService;
        _cache = cache;
        if (environment.IsDevelopment())
        {
            _localizationDirectoryUI = directoryService.FileSystem.Path.Join(
                directoryService.FileSystem.Directory.GetCurrentDirectory(),
                "UI/Web/src/assets/langs");
        }
        else
        {
            _localizationDirectoryUI = directoryService.FileSystem.Path.Join(
                directoryService.FileSystem.Directory.GetCurrentDirectory(),
                "wwwroot", "assets/langs");
        }

        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
    }

    /// <summary>
    /// Loads a language
    /// </summary>
    /// <param name="languageCode"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>?> LoadLanguage(string languageCode)
    {
        var languageFile = _directoryService.FileSystem.Path.Join(_directoryService.LocalizationDirectory, languageCode + ".json");
        if (!_directoryService.FileSystem.FileInfo.New(languageFile).Exists)
            throw new ArgumentException($"Language {languageCode} does not exist");

        var json = await _directoryService.FileSystem.File.ReadAllTextAsync(languageFile);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    public async Task<string> Get(string locale, string key, params object[] args)
    {

        // Check if the translation for the given locale is cached
        if (!_cache.TryGetValue($"{locale}_{key}", out string? translatedString))
        {
            // Load the locale JSON file
            var translationData = await LoadLanguage(locale);

            // Find the translation for the given key
            if (translationData != null && translationData.TryGetValue(key, out var value))
            {
                translatedString = value;

                // Cache the translation for subsequent requests
                _cache.Set($"{locale}_{key}", translatedString, _cacheOptions);
            }
        }


        if (string.IsNullOrEmpty(translatedString))
        {
            if (!locale.Equals("en"))
            {
                return await Get("en", key, args);
            }
            return key;
        }

        // Format the translated string with arguments
        if (args.Length > 0)
        {
            translatedString = string.Format(translatedString, args);
        }

        return translatedString;
    }


    /// <summary>
    /// Returns all available locales that exist on both the Frontend and the Backend
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetLocales()
    {
        return
            _directoryService.GetFilesWithExtension(_directoryService.FileSystem.Path.GetFullPath(_localizationDirectoryUI), @"\.json")
                .Select(f => _directoryService.FileSystem.Path.GetFileName(f).Replace(".json", string.Empty))
            .Union(_directoryService.GetFilesWithExtension(_directoryService.LocalizationDirectory, @"\.json")
                .Select(f => _directoryService.FileSystem.Path.GetFileName(f).Replace(".json", string.Empty)))
                .Distinct();
    }
}
