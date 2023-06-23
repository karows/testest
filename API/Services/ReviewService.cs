﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.Data;
using API.Data.Repositories;
using API.DTOs.SeriesDetail;
using API.Entities;
using API.Entities.Enums;
using API.Helpers;
using API.Services.Plus;
using Flurl.Http;
using HtmlAgilityPack;
using Kavita.Common;
using Kavita.Common.EnvironmentInfo;
using Kavita.Common.Helpers;
using Microsoft.Extensions.Logging;

namespace API.Services;

internal class MediaReviewDto
{
    public string Body { get; set; }
    public string Tagline { get; set; }
    public int Rating { get; set; }
    public int TotalVotes { get; set; }
    /// <summary>
    /// The media's overall Score
    /// </summary>
    public int Score { get; set; }
    public string SiteUrl { get; set; }
    /// <summary>
    /// In Markdown
    /// </summary>
    public string RawBody { get; set; }
    public string Username { get; set; }
}

public interface IReviewService
{
    Task<IEnumerable<UserReviewDto>> GetReviewsForSeries(int userId, int seriesId);
}

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewService> _logger;


    public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;

        FlurlHttp.ConfigureClient(Configuration.KavitaPlusApiUrl, cli =>
            cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());
    }

    public async Task<IEnumerable<UserReviewDto>> GetReviewsForSeries(int userId, int seriesId)
    {
        var series =
            await _unitOfWork.SeriesRepository.GetSeriesByIdAsync(seriesId,
                SeriesIncludes.Metadata | SeriesIncludes.Library | SeriesIncludes.Chapters | SeriesIncludes.Volumes);
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(userId);
        if (user == null || series == null) return new List<UserReviewDto>();
        var license = await _unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.LicenseKey);
        var ret = (await GetReviews(license.Value, series)).Select(r => new UserReviewDto()
        {
            Body = r.Body,
            Tagline = r.Tagline,
            Score = r.Score,
            Username = r.Username,
            LibraryId = series.LibraryId,
            SeriesId = series.Id,
            IsExternal = true,
            BodyJustText = GetCharacters(r.Body),
            ExternalUrl = r.SiteUrl
        });

        return ret.OrderByDescending(r => r.Score);
    }

    private static string GetCharacters(string body)
    {
        if (string.IsNullOrEmpty(body)) return body;

        var doc = new HtmlDocument();
        doc.LoadHtml(body);

        var textNodes = doc.DocumentNode.SelectNodes("//text()[not(parent::script)]");
        if (textNodes == null) return string.Empty;
        var plainText =  string.Join(" ", textNodes
            .Select(node => node.InnerText)
            .Where(s => !s.Equals("\n")));

        // Clean any leftover markdown out
        plainText = Regex.Replace(plainText, @"[_*\[\]~]", string.Empty);
        plainText = Regex.Replace(plainText, @"img\d*\((.*?)\)", string.Empty);
        plainText = Regex.Replace(plainText, @"~~~(.*?)~~~", "$1");
        plainText = Regex.Replace(plainText, @"\+{3}(.*?)\+{3}", "$1");
        plainText = Regex.Replace(plainText, @"~~(.*?)~~", "$1");
        plainText = Regex.Replace(plainText, @"__(.*?)__", "$1");
        plainText = Regex.Replace(plainText, @"#\s(.*?)", "$1");

        // Just strip symbols
        plainText = Regex.Replace(plainText, @"[_*\[\]~]", string.Empty);
        plainText = Regex.Replace(plainText, @"img\d*\((.*?)\)", string.Empty);
        plainText = Regex.Replace(plainText, @"~~~", string.Empty);
        plainText = Regex.Replace(plainText, @"\+", string.Empty);
        plainText = Regex.Replace(plainText, @"~~", string.Empty);
        plainText = Regex.Replace(plainText, @"__", string.Empty);

        // Take the first 100 characters
        plainText = plainText.Length > 100 ? plainText.Substring(0, 100) : plainText;

        return plainText + "…";
    }


    private async Task<IEnumerable<MediaReviewDto>> GetReviews(string license, Series series)
    {
        _logger.LogDebug("Fetching external reviews for Series: {SeriesName}", series.Name);
        try
        {
            return await (Configuration.KavitaPlusApiUrl + "/api/review")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "Kavita")
                .WithHeader("x-license-key", license)
                .WithHeader("x-installId", HashUtil.ServerToken())
                .WithHeader("x-kavita-version", BuildInfo.Version)
                .WithHeader("Content-Type", "application/json")
                .WithTimeout(TimeSpan.FromSeconds(Configuration.DefaultTimeOutSecs))
                .PostJsonAsync(new PlusSeriesDto()
                {
                    MediaFormat = LibraryTypeHelper.GetFormat(series.Library.Type),
                    SeriesName = series.Name,
                    AltSeriesName = series.LocalizedName,
                    AniListId = ScrobblingService.ExtractId(series.Metadata.WebLinks,
                        ScrobblingService.AniListWeblinkWebsite),
                    MalId = ScrobblingService.ExtractId(series.Metadata.WebLinks,
                        ScrobblingService.MalWeblinkWebsite),
                    VolumeCount = series.Volumes.Count,
                    ChapterCount = series.Volumes.SelectMany(v => v.Chapters).Count(c => !c.IsSpecial),
                    Year = series.Metadata.ReleaseYear
                })
                .ReceiveJson<IEnumerable<MediaReviewDto>>();

        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error happened during the request to KavitaPlus API");
        }

        return new List<MediaReviewDto>();
    }
}
