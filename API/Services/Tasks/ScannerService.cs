﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using API.Comparators;
using API.Entities;
using API.Entities.Enums;
using API.Entities.Interfaces;
using API.Extensions;
using API.Interfaces;
using API.Interfaces.Services;
using API.Parser;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace API.Services.Tasks
{
    public class ScannerService : IScannerService
    {
       private readonly IUnitOfWork _unitOfWork;
       private readonly ILogger<ScannerService> _logger;
       private readonly IArchiveService _archiveService;
       private readonly IMetadataService _metadataService;
       private readonly IBookService _bookService;
       private ConcurrentDictionary<string, List<ParserInfo>> _scannedSeries;
       private readonly NaturalSortComparer _naturalSort;

       public ScannerService(IUnitOfWork unitOfWork, ILogger<ScannerService> logger, IArchiveService archiveService, 
          IMetadataService metadataService, IBookService bookService)
       {
          _unitOfWork = unitOfWork;
          _logger = logger;
          _archiveService = archiveService;
          _metadataService = metadataService;
          _bookService = bookService;
          _naturalSort = new NaturalSortComparer();
       }


       [DisableConcurrentExecution(timeoutInSeconds: 360)]
       public void ScanLibraries()
       {
          var libraries = Task.Run(() => _unitOfWork.LibraryRepository.GetLibrariesAsync()).Result.ToList();
          foreach (var lib in libraries)
          {
             // BUG?: I think we need to keep _scannedSeries within the ScanLibrary instance since this is multithreaded.
             ScanLibrary(lib.Id, false);
          }
       }

       private bool ShouldSkipFolderScan(FolderPath folder, ref int skippedFolders)
       {
          // NOTE: The only way to skip folders is if Directory hasn't been modified, we aren't doing a forcedUpdate and version hasn't changed between scans.
          return false;

          // if (!_forceUpdate && Directory.GetLastWriteTime(folder.Path) < folder.LastScanned)
          // {
          //    _logger.LogDebug("{FolderPath} hasn't been modified since last scan. Skipping", folder.Path);
          //    skippedFolders += 1;
          //    return true;
          // }
          
          //return false;
       }

       [DisableConcurrentExecution(360)]
       public void ScanLibrary(int libraryId, bool forceUpdate)
       {
          var sw = Stopwatch.StartNew();
          _scannedSeries = new ConcurrentDictionary<string, List<ParserInfo>>();
          Library library;
           try
           {
              library = Task.Run(() => _unitOfWork.LibraryRepository.GetFullLibraryForIdAsync(libraryId)).GetAwaiter().GetResult();
           }
           catch (Exception ex)
           {
              // This usually only fails if user is not authenticated.
              _logger.LogError(ex, "There was an issue fetching Library {LibraryId}", libraryId);
              return;
           }
           
           
           var series = ScanLibrariesForSeries(forceUpdate, library, sw, out var totalFiles, out var scanElapsedTime);
           UpdateLibrary(library, series);
           
           _unitOfWork.LibraryRepository.Update(library);
           if (Task.Run(() => _unitOfWork.Complete()).Result)
           {
              _logger.LogInformation("Processed {TotalFiles} files and {ParsedSeriesCount} series in {ElapsedScanTime} milliseconds for {LibraryName}", totalFiles, series.Keys.Count, sw.ElapsedMilliseconds + scanElapsedTime, library.Name);
           }
           else
           {
              _logger.LogCritical("There was a critical error that resulted in a failed scan. Please check logs and rescan");
           }

           CleanupUserProgress();

           BackgroundJob.Enqueue(() => _metadataService.RefreshMetadata(libraryId, forceUpdate));
       }

       /// <summary>
       /// Remove any user progress rows that no longer exist since scan library ran and deleted series/volumes/chapters
       /// </summary>
       private void CleanupUserProgress()
       {
          var cleanedUp = Task.Run(() => _unitOfWork.AppUserProgressRepository.CleanupAbandonedChapters()).Result;
          _logger.LogInformation("Removed {Count} abandoned progress rows", cleanedUp);
       }

       private Dictionary<string, List<ParserInfo>> ScanLibrariesForSeries(bool forceUpdate, Library library, Stopwatch sw, out int totalFiles,
          out long scanElapsedTime)
       {
          _logger.LogInformation("Beginning scan on {LibraryName}. Forcing metadata update: {ForceUpdate}", library.Name,
             forceUpdate);
          totalFiles = 0;
          var skippedFolders = 0;
          foreach (var folderPath in library.Folders)
          {
             if (ShouldSkipFolderScan(folderPath, ref skippedFolders)) continue;

             // NOTE: we can refactor this to allow all filetypes and handle everything in the ProcessFile to allow mixed library types.
             var searchPattern = Parser.Parser.ArchiveFileExtensions;
             if (library.Type == LibraryType.Book)
             {
                searchPattern = Parser.Parser.BookFileExtensions;
             }

             try
             {
                totalFiles += DirectoryService.TraverseTreeParallelForEach(folderPath.Path, (f) =>
                {
                   try
                   {
                      ProcessFile(f, folderPath.Path, library.Type);
                   }
                   catch (FileNotFoundException exception)
                   {
                      _logger.LogError(exception, "The file {Filename} could not be found", f);
                   }
                }, searchPattern);
             }
             catch (ArgumentException ex)
             {
                _logger.LogError(ex, "The directory '{FolderPath}' does not exist", folderPath.Path);
             }

             folderPath.LastScanned = DateTime.Now;
          }

          scanElapsedTime = sw.ElapsedMilliseconds;
          _logger.LogInformation("Folders Scanned {TotalFiles} files in {ElapsedScanTime} milliseconds", totalFiles,
             scanElapsedTime);
          sw.Restart();
          if (skippedFolders == library.Folders.Count)
          {
             _logger.LogInformation("All Folders were skipped due to no modifications to the directories");
             _unitOfWork.LibraryRepository.Update(library);
             _scannedSeries = null;
             _logger.LogInformation("Processed {TotalFiles} files in {ElapsedScanTime} milliseconds for {LibraryName}",
                totalFiles, sw.ElapsedMilliseconds, library.Name);
             return new Dictionary<string, List<ParserInfo>>();
          }
          
          return SeriesWithInfos(_scannedSeries);
       }

       /// <summary>
       /// Returns any series where there were parsed infos
       /// </summary>
       /// <param name="scannedSeries"></param>
       /// <returns></returns>
       private Dictionary<string, List<ParserInfo>> SeriesWithInfos(IDictionary<string, List<ParserInfo>> scannedSeries)
       {
          var filtered = scannedSeries.Where(kvp => kvp.Value.Count > 0);
          var series = filtered.ToDictionary(v => v.Key, v => v.Value);
          return series;
       }

       
       private void UpdateLibrary(Library library, Dictionary<string, List<ParserInfo>> parsedSeries)
       {
          if (parsedSeries == null) throw new ArgumentNullException(nameof(parsedSeries));
          
          // First, remove any series that are not in parsedSeries list
          var missingSeries = FindSeriesNotOnDisk(library.Series, parsedSeries);
          var removeCount = RemoveMissingSeries(library.Series, missingSeries);
          _logger.LogInformation("Removed {RemoveMissingSeries} series that are no longer on disk", removeCount);
          
          // Add new series that have parsedInfos
          foreach (var (key, infos) in parsedSeries)
          {
             var existingSeries = library.Series.SingleOrDefault(s => s.NormalizedName == Parser.Parser.Normalize(key));
             if (existingSeries == null)
             {
                var name = infos.Count > 0 ? infos[0].Series : key;
                existingSeries = new Series()
                {
                   Name = name,
                   OriginalName = name,
                   LocalizedName = name,
                   NormalizedName = Parser.Parser.Normalize(key),
                   SortName = key,
                   Summary = "",
                   Volumes = new List<Volume>()
                };
                library.Series.Add(existingSeries);
             } 
             existingSeries.NormalizedName = Parser.Parser.Normalize(key);
             existingSeries.LocalizedName ??= key;
          }

          // Now, we only have to deal with series that exist on disk. Let's recalculate the volumes for each series
          var librarySeries = library.Series.ToList();
          // Parallel.ForEach(librarySeries, (series) =>
          // {
          //    try
          //    {
          //       _logger.LogInformation("Processing series {SeriesName}", series.OriginalName);
          //       UpdateVolumes(series, parsedSeries[Parser.Parser.Normalize(series.OriginalName)].ToArray());
          //       series.Pages = series.Volumes.Sum(v => v.Pages);
          //    }
          //    catch (Exception ex)
          //    {
          //       _logger.LogError(ex, "There was an exception updating volumes for {SeriesName}", series.Name);
          //    }
          // });
          // TODO: Remove this debug code
          foreach (var series in library.Series)
          {
             try
             {
                _logger.LogInformation("Processing series {SeriesName}", series.OriginalName);
                UpdateVolumes(series, parsedSeries[Parser.Parser.Normalize(series.OriginalName)].ToArray());
                series.Pages = series.Volumes.Sum(v => v.Pages);
             }
             catch (Exception ex)
             {
                _logger.LogError(ex, "There was an exception updating volumes for {SeriesName}", series.Name);
             }
          }
       }

       public IEnumerable<Series> FindSeriesNotOnDisk(ICollection<Series> existingSeries, Dictionary<string, List<ParserInfo>> parsedSeries)
       {
          var foundSeries = parsedSeries.Select(s => s.Key).ToList();
          var missingSeries = existingSeries.Where(es => !es.NameInList(foundSeries)
                                                                     || !es.NameInList(parsedSeries.Keys));
          return missingSeries;
       }

       public int RemoveMissingSeries(ICollection<Series> existingSeries, IEnumerable<Series> missingSeries)
       {
          
          var removeCount = existingSeries.Count;
          var missingList = missingSeries.ToList();
          existingSeries = existingSeries.Except(missingList).ToList();
          removeCount -= existingSeries.Count;

          return removeCount;
       }

       private void UpdateVolumes(Series series, ParserInfo[] parsedInfos)
       {
          var startingVolumeCount = series.Volumes.Count;
          // Add new volumes and update chapters per volume
          var distinctVolumes = parsedInfos.DistinctVolumes();
          _logger.LogDebug("Updating {DistinctVolumes} volumes", distinctVolumes.Count);
          foreach (var volumeNumber in distinctVolumes)
          {
             var volume = series.Volumes.SingleOrDefault(s => s.Name == volumeNumber);
             if (volume == null)
             {
                volume = new Volume()
                {
                   Name = volumeNumber,
                   Number = (int) Parser.Parser.MinimumNumberFromRange(volumeNumber),
                   Chapters = new List<Chapter>()
                }; 
                series.Volumes.Add(volume);
             }
             
             _logger.LogDebug("Parsing {SeriesName} - Volume {VolumeNumber}", series.Name, volume.Name);
             var infos = parsedInfos.Where(p => p.Volumes == volumeNumber).ToArray();
             UpdateChapters(volume, infos);
             volume.Pages = volume.Chapters.Sum(c => c.Pages);
          }
          
          // Remove existing volumes that aren't in parsedInfos or volumes that have no chapters
          var existingVolumeLength = series.Volumes.Count;
          // BUG: ParsedInfos aren't coming when files are still on disk causing removal of volumes then re-added on another scan.
          //var deletedVolumes = series.Volumes.Where(v => !parsedInfos.Select(p => p.Volumes).Contains(v.Name)).ToList();
          var nonDeletedVolumes = series.Volumes.Where(v => parsedInfos.Select(p => p.Volumes).Contains(v.Name)).ToList();
          if (series.Volumes.Count != nonDeletedVolumes.Count)
          {
             _logger.LogDebug("Removed {Count} volumes from {SeriesName} where parsed infos were not mapping with volume name",
                (series.Volumes.Count - nonDeletedVolumes.Count), series.Name);
             var deletedVolumes = series.Volumes.Except(nonDeletedVolumes);
             foreach (var volume in deletedVolumes)
             {
                var file = volume.Chapters.FirstOrDefault()?.Files.FirstOrDefault()?.FilePath ?? "no files";
                if (!new FileInfo(file).Exists)
                {
                   _logger.LogError("Volume cleanup code was trying to remove a volume with a file still existing on disk. File: {File}", file);
                }
                _logger.LogDebug("Removed {SeriesName} - Volume {Volume}: {File}", series.Name, volume.Name, file);
             }

             series.Volumes = nonDeletedVolumes;
          }

          _logger.LogDebug("Updated {SeriesName} volumes from {StartingVolumeCount} to {VolumeCount}", 
             series.Name, startingVolumeCount, series.Volumes.Count);
       }
       
       private void UpdateChapters(Volume volume, ParserInfo[] parsedInfos)
       {
          var startingChapters = volume.Chapters.Count;

          // Add new chapters
          foreach (var info in parsedInfos)
          {
             var specialTreatment = info.IsSpecialInfo();
             // Specials go into their own chapters with Range being their filename and IsSpecial = True. Non-Specials with Vol and Chap as 0
             // also are treated like specials for UI grouping.
             Chapter chapter;
             try
             {
                // chapter = specialTreatment
                //    ? volume.Chapters.SingleOrDefault(c => c.Range == info.Filename
                //                                           || (c.Files.Select(f => f.FilePath)
                //                                              .Contains(info.FullFilePath)))
                //    : volume.Chapters.SingleOrDefault(c => c.Range == info.Chapters);
                chapter = volume.Chapters.GetChapterByRange(info);
             }
             catch (Exception ex)
             {
                _logger.LogError(ex, "{FileName} mapped as '{Series} - Vol {Volume} Ch {Chapter}' is a duplicate, skipping", info.FullFilePath, info.Series, info.Volumes, info.Chapters);
                continue;
             }
             
             if (chapter == null)
             {
                _logger.LogDebug(
                   "Adding new chapter, {Series} - Vol {Volume} Ch {Chapter} - Needs Special Treatment? {NeedsSpecialTreatment}",
                   info.Series, info.Volumes, info.Chapters, specialTreatment);
                volume.Chapters.Add(CreateChapter(info));
             }
             else
             {
                chapter.Files ??= new List<MangaFile>();
                chapter.IsSpecial = specialTreatment;
                chapter.Title = (specialTreatment && info.Format == MangaFormat.Book)
                   ? info.Title
                   : chapter.Range;
                //Problem is that we need a merge. Because old chapter might need new code generation
             }
             
          }
          
          // Add files
          foreach (var info in parsedInfos)
          {
             var specialTreatment = info.IsSpecialInfo();
             Chapter chapter = null;
             try
             {
                chapter = volume.Chapters.GetAnyChapterByRange(info);
             }
             catch (Exception ex)
             {
                _logger.LogError(ex, "There was an exception parsing chapter. Skipping {SeriesName} Vol {VolumeNumber} Chapter {ChapterNumber} - Special treatment: {NeedsSpecialTreatment}", info.Series, volume.Name, info.Chapters, specialTreatment);
                continue;
             }
             if (chapter == null) continue;
             AddOrUpdateFileForChapter(chapter, info);
             chapter.Number = Parser.Parser.MinimumNumberFromRange(info.Chapters) + string.Empty;
             chapter.Range = specialTreatment ? info.Filename : info.Chapters;
             chapter.Pages = chapter.Files.Sum(f => f.Pages);
          }
          
          
          // Remove chapters that aren't in parsedInfos or have no files linked
          // TODO: See if we can use Except() here
          var existingChapters = volume.Chapters.ToList();
          foreach (var existingChapter in existingChapters)
          {
             if (existingChapter.Files.Count == 0 || !parsedInfos.HasInfo(existingChapter))
             {
                _logger.LogDebug("Removed chapter {Chapter} for Volume {VolumeNumber} on {SeriesName}", existingChapter.Range, volume.Name, parsedInfos[0].Series);
                volume.Chapters.Remove(existingChapter);
             }
             else
             {
                // Ensure we remove any files that no longer exist AND order
                existingChapter.Files = existingChapter.Files
                   .Where(f => parsedInfos.Any(p => p.FullFilePath == f.FilePath))
                   .OrderBy(f => f.FilePath, _naturalSort).ToList();
             }
          }

          _logger.LogDebug("Updated chapters from {StartingChaptersCount} to {ChapterCount}", 
             startingChapters, volume.Chapters.Count);
       }

       private Chapter CreateChapter(ParserInfo info)
       {
          var specialTreatment = info.IsSpecialInfo();
          var specialTitle = specialTreatment ? info.Filename : info.Chapters;
          return new Chapter()
          {
             Number = Parser.Parser.MinimumNumberFromRange(info.Chapters) + string.Empty,
             Range = specialTreatment ? info.Filename : info.Chapters,
             Title = (specialTreatment && info.Format == MangaFormat.Book)
                ? info.Title
                : specialTitle,
             Files = new List<MangaFile>(),
             IsSpecial = specialTreatment
          };
       }


       /// <summary>
       /// Attempts to either add a new instance of a show mapping to the _scannedSeries bag or adds to an existing.
       /// </summary>
       /// <param name="info"></param>
       private void TrackSeries(ParserInfo info)
       {
          if (info.Series == string.Empty) return;
          
          // Check if normalized info.Series already exists and if so, update info to use that name instead
          info.Series = MergeName(_scannedSeries, info);
          
          _scannedSeries.AddOrUpdate(Parser.Parser.Normalize(info.Series), new List<ParserInfo>() {info}, (_, oldValue) =>
          {
             oldValue ??= new List<ParserInfo>();
             if (!oldValue.Contains(info))
             {
                oldValue.Add(info);
             }

             return oldValue;
          });
       }

       public string MergeName(ConcurrentDictionary<string,List<ParserInfo>> collectedSeries, ParserInfo info)
       {
          var normalizedSeries = Parser.Parser.Normalize(info.Series);
          _logger.LogDebug("Checking if we can merge {NormalizedSeries}", normalizedSeries);
          var existingName = collectedSeries.SingleOrDefault(p => Parser.Parser.Normalize(p.Key) == normalizedSeries)
             .Key;
          if (!string.IsNullOrEmpty(existingName) && info.Series != existingName)
          {
             _logger.LogDebug("Found duplicate parsed infos, merged {Original} into {Merged}", info.Series, existingName);
             return existingName;
          }

          return info.Series;
       }

       /// <summary>
       /// Processes files found during a library scan.
       /// Populates a collection of <see cref="ParserInfo"/> for DB updates later.
       /// </summary>
       /// <param name="path">Path of a file</param>
       /// <param name="rootPath"></param>
       /// <param name="type">Library type to determine parsing to perform</param>
       private void ProcessFile(string path, string rootPath, LibraryType type)
       {
          var info = Parser.Parser.Parse(path, rootPath, type);

          // Problem: Bad parsing on non-manga books cause skewing to metadata.
          if (type == LibraryType.Book && Parser.Parser.IsEpub(path))
          {
             var info2 = BookService.ParseInfo(path);
             if (info != null)
             {
                info2.MergeFrom(info);
             }

             info = info2;
          }

          if (info == null)
          {
             _logger.LogWarning("[Scanner] Could not parse series from {Path}", path);
             return;
          }
          
          TrackSeries(info);
       }

       private MangaFile CreateMangaFile(ParserInfo info)
       {
          switch (info.Format)
          {
             case MangaFormat.Archive:
             {
                return new MangaFile()
                {
                   FilePath = info.FullFilePath,
                   Format = info.Format,
                   Pages = _archiveService.GetNumberOfPagesFromArchive(info.FullFilePath)
                };
             }
             case MangaFormat.Book:
             {
                return new MangaFile()
                {
                   FilePath = info.FullFilePath,
                   Format = info.Format,
                   Pages = _bookService.GetNumberOfPages(info.FullFilePath)
                };
             }
             default:
                _logger.LogWarning("[Scanner] Ignoring {Filename}. Non-archives are not supported", info.Filename);
                break;
          }

          return null;
       }
  
       private void AddOrUpdateFileForChapter(Chapter chapter, ParserInfo info)
       {
          chapter.Files ??= new List<MangaFile>();
          var existingFile = chapter.Files.SingleOrDefault(f => f.FilePath == info.FullFilePath);
          if (existingFile != null)
          {
             existingFile.Format = info.Format;
             if (!existingFile.HasFileBeenModified())
             {
                existingFile.Pages = _archiveService.GetNumberOfPagesFromArchive(info.FullFilePath);
             }
          }
          else
          {
             var file = CreateMangaFile(info);
             if (file != null)
             {
                chapter.Files.Add(file);
                existingFile = chapter.Files.Last();
             }
          }

          if (existingFile != null)
          {
             existingFile.LastModified = new FileInfo(existingFile.FilePath).LastWriteTime;
          }
       }
    }
}