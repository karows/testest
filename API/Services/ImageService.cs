﻿using System;
using System.IO;
using System.Linq;
using API.Comparators;
using API.Entities;
using API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NetVips;

namespace API.Services
{

  public class ImageService : IImageService
  {
    private readonly ILogger<ImageService> _logger;
    private readonly IDirectoryService _directoryService;

    /// <summary>
    /// Width of the Thumbnail generation
    /// </summary>
    private const int ThumbnailWidth = 320;

    public ImageService(ILogger<ImageService> logger, IDirectoryService directoryService)
    {
      _logger = logger;
      _directoryService = directoryService;
    }

    /// <summary>
    /// Finds the first image in the directory of the first file. Does not check for "cover/folder".ext files to override.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public string GetCoverFile(MangaFile file)
    {
      var directory = Path.GetDirectoryName(file.FilePath);
      if (string.IsNullOrEmpty(directory))
      {
        _logger.LogError("Could not find Directory for {File}", file.FilePath);
        return null;
      }

      var firstImage = _directoryService.GetFilesWithExtension(directory, Parser.Parser.ImageFileExtensions)
        .OrderBy(f => f, new NaturalSortComparer()).FirstOrDefault();

      return firstImage;
    }

    public string GetCoverImage(string path, string fileName)
    {
      if (string.IsNullOrEmpty(path)) return string.Empty;

      try
      {
          return CreateThumbnail(path,  fileName);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "[GetCoverImage] There was an error and prevented thumbnail generation on {ImageFile}. Defaulting to no cover image", path);
      }

      return string.Empty;
    }

    /// <inheritdoc />
    public string CreateThumbnail(string path, string fileName)
    {
        try
        {
            using var thumbnail = Image.Thumbnail(path, ThumbnailWidth);
            var filename = fileName + ".png";
            thumbnail.WriteToFile(Path.Join(DirectoryService.CoverImageDirectory, fileName + ".png"));
            return filename;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating thumbnail from url");
        }

        return string.Empty;
    }

    /// <summary>
    /// Creates a thumbnail out of a memory stream and saves to <see cref="DirectoryService.CoverImageDirectory"/> with the passed
    /// fileName and .png extension.
    /// </summary>
    /// <param name="stream">Stream to write to disk. Ensure this is rewinded.</param>
    /// <param name="fileName">filename to save as without extension</param>
    /// <returns>File name with extension of the file. This will always write to <see cref="DirectoryService.CoverImageDirectory"/></returns>
    public static string WriteCoverThumbnail(Stream stream, string fileName)
    {
        using var thumbnail = NetVips.Image.ThumbnailStream(stream, ThumbnailWidth);
        // using var sha1 = new System.Security.Cryptography.SHA256CryptoServiceProvider();
        // string.Concat(sha1.ComputeHash(content).Select(x => x.ToString("X2")))
        var filename = fileName + ".png";
        thumbnail.WriteToFile(Path.Join(DirectoryService.CoverImageDirectory, fileName + ".png"));
        return filename;
    }


    /// <inheritdoc />
    public string CreateThumbnailFromBase64(string encodedImage, string fileName)
    {
        try
        {
            using var thumbnail = Image.ThumbnailBuffer(Convert.FromBase64String(encodedImage), ThumbnailWidth);
            var filename = fileName + ".png";
            thumbnail.WriteToFile(Path.Join(DirectoryService.CoverImageDirectory, fileName + ".png"));
            return filename;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating thumbnail from url");
        }

        return string.Empty;
    }

    public static string GetChapterFormat(int chapterId, int volumeId)
    {
        return $"v{volumeId}_c{chapterId}";
    }


    public static string GetSeriesFormat(int seriesId)
    {
        return $"series{seriesId}";
    }

    public static string GetCollectionTagFormat(int tagId)
    {
        return $"tag{tagId}";
    }
  }
}
