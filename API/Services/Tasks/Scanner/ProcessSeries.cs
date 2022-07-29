﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Data.Metadata;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using API.Helpers;
using API.Parser;
using API.SignalR;
using Microsoft.Extensions.Logging;

namespace API.Services.Tasks.Scanner;

public interface IProcessSeries
{
    Task ProcessSeriesAsync(IList<ParserInfo> parsedInfos, BlockingCollection<Person> allPeople, BlockingCollection<Tag> allTags, BlockingCollection<Genre> allGenres, Library library);
}

/// <summary>
/// All code needed to Update a Series from a Scan action
/// </summary>
public class ProcessSeries : IProcessSeries
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessSeries> _logger;
    private readonly IEventHub _eventHub;
    private readonly IDirectoryService _directoryService;
    private readonly ICacheHelper _cacheHelper;
    private readonly IReadingItemService _readingItemService;
    private readonly IFileService _fileService;

    public ProcessSeries(IUnitOfWork unitOfWork, ILogger<ProcessSeries> logger, IEventHub eventHub,
        IDirectoryService directoryService, ICacheHelper cacheHelper, IReadingItemService readingItemService,
        IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _eventHub = eventHub;
        _directoryService = directoryService;
        _cacheHelper = cacheHelper;
        _readingItemService = readingItemService;
        _fileService = fileService;
    }

    public async Task ProcessSeriesAsync(IList<ParserInfo> parsedInfos, BlockingCollection<Person> allPeople, BlockingCollection<Tag> allTags, BlockingCollection<Genre> allGenres, Library library)
    {
        if (!parsedInfos.Any()) return;

        var scanWatch = Stopwatch.StartNew();
        var seriesName = parsedInfos.First().Series;
        _logger.LogInformation("[ScannerService] Beginning series update on {SeriesName}", seriesName);

        // Check if there is a Series
        var series = await _unitOfWork.SeriesRepository.GetFullSeriesByName(parsedInfos.First().Series, library.Id) ?? DbFactory.Series(parsedInfos.First().Series);

        if (series.LibraryId == 0) series.LibraryId = library.Id;

        await _eventHub.SendMessageAsync(MessageFactory.NotificationProgress, MessageFactory.LibraryScanProgressEvent(library.Name, ProgressEventType.Updated, series.Name));
        try
        {
            _logger.LogInformation("[ScannerService] Processing series {SeriesName}", series.OriginalName);
            //await _eventHub.SendMessageAsync(MessageFactory.NotificationProgress, MessageFactory.LibraryScanProgressEvent(library.Name, ProgressEventType.Ended, series.Name));

            // Get all associated ParsedInfos to the series. This includes infos that use a different filename that matches Series LocalizedName

            UpdateVolumes(series, parsedInfos, allPeople, allTags, allGenres);
            series.Pages = series.Volumes.Sum(v => v.Pages);

            series.NormalizedName = Parser.Parser.Normalize(series.Name);
            series.OriginalName ??= parsedInfos[0].Series;
            if (series.Format == MangaFormat.Unknown)
            {
                series.Format = parsedInfos[0].Format;
            }






            if (string.IsNullOrEmpty(series.SortName))
            {
                series.SortName = series.Name;
            }
            if (!series.SortNameLocked)
            {
                series.SortName = series.Name;
                if (!string.IsNullOrEmpty(parsedInfos[0].SeriesSort))
                {
                    series.SortName = parsedInfos[0].SeriesSort;
                }
            }

            // parsedInfos[0] is not the first volume or chapter. We need to find it
            var localizedSeries = parsedInfos.Select(p => p.LocalizedSeries).FirstOrDefault(p => !string.IsNullOrEmpty(p));
            if (!series.LocalizedNameLocked && !string.IsNullOrEmpty(localizedSeries))
            {
                series.LocalizedName = localizedSeries;
            }

            // Update series FolderPath here (TODO: Move this into it's own private method)
            var seriesDirs = _directoryService.FindHighestDirectoriesFromFiles(library.Folders.Select(l => l.Path), parsedInfos.Select(f => f.FullFilePath).ToList());
            if (seriesDirs.Keys.Count == 0)
            {
                _logger.LogCritical("Scan Series has files spread outside a main series folder. This has negative performance effects. Please ensure all series are in a folder");
            }
            else
            {
                // Don't save FolderPath if it's a library Folder
                if (!library.Folders.Select(f => f.Path).Contains(seriesDirs.Keys.First()))
                {
                    series.FolderPath = Parser.Parser.NormalizePath(seriesDirs.Keys.First());
                }
            }

            series.Metadata ??= DbFactory.SeriesMetadata(new List<CollectionTag>());
            UpdateSeriesMetadata(series, allPeople, allGenres, allTags, library.Type);

            series.LastFolderScanned = DateTime.UtcNow;
            _unitOfWork.SeriesRepository.Attach(series);

            try
            {
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[ScannerService] There was an issue writing to the for series {@SeriesName}", series);

                await _eventHub.SendMessageAsync(MessageFactory.Error,
                    MessageFactory.ErrorEvent($"There was an issue writing to the DB for Series {series}",
                        string.Empty));
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScannerService] There was an exception updating volumes for {SeriesName}", series.Name);
        }

        //await _eventHub.SendMessageAsync(MessageFactory.NotificationProgress, MessageFactory.LibraryScanProgressEvent(library.Name, ProgressEventType.Ended, series.Name));

        _logger.LogInformation("[ScannerService] Finished series update on {SeriesName} in {Milliseconds} ms", seriesName, scanWatch.ElapsedMilliseconds);

    }

    private static void UpdateSeriesMetadata(Series series, BlockingCollection<Person> allPeople, BlockingCollection<Genre> allGenres, BlockingCollection<Tag> allTags, LibraryType libraryType)
    {
        var isBook = libraryType == LibraryType.Book;
        var firstChapter = SeriesService.GetFirstChapterForMetadata(series, isBook);

        var firstFile = firstChapter?.Files.FirstOrDefault();
        if (firstFile == null) return;
        if (Parser.Parser.IsPdf(firstFile.FilePath)) return;

        var chapters = series.Volumes.SelectMany(volume => volume.Chapters).ToList();

        // Update Metadata based on Chapter metadata
        series.Metadata.ReleaseYear = chapters.Min(c => c.ReleaseDate.Year);

        if (series.Metadata.ReleaseYear < 1000)
        {
            // Not a valid year, default to 0
            series.Metadata.ReleaseYear = 0;
        }

        // Set the AgeRating as highest in all the comicInfos
        if (!series.Metadata.AgeRatingLocked) series.Metadata.AgeRating = chapters.Max(chapter => chapter.AgeRating);

        series.Metadata.TotalCount = chapters.Max(chapter => chapter.TotalCount);
        series.Metadata.MaxCount = chapters.Max(chapter => chapter.Count);
        // To not have to rely completely on ComicInfo, try to parse out if the series is complete by checking parsed filenames as well.
        if (series.Metadata.MaxCount != series.Metadata.TotalCount)
        {
            var maxVolume = series.Volumes.Max(v => (int) Parser.Parser.MaxNumberFromRange(v.Name));
            var maxChapter = chapters.Max(c => (int) Parser.Parser.MaxNumberFromRange(c.Range));
            if (maxVolume == series.Metadata.TotalCount) series.Metadata.MaxCount = maxVolume;
            else if (maxChapter == series.Metadata.TotalCount) series.Metadata.MaxCount = maxChapter;
        }


        if (!series.Metadata.PublicationStatusLocked)
        {
            series.Metadata.PublicationStatus = PublicationStatus.OnGoing;
            if (series.Metadata.MaxCount >= series.Metadata.TotalCount && series.Metadata.TotalCount > 0)
            {
                series.Metadata.PublicationStatus = PublicationStatus.Completed;
            } else if (series.Metadata.TotalCount > 0 && series.Metadata.MaxCount > 0)
            {
                series.Metadata.PublicationStatus = PublicationStatus.Ended;
            }
        }

        if (!string.IsNullOrEmpty(firstChapter.Summary) && !series.Metadata.SummaryLocked)
        {
            series.Metadata.Summary = firstChapter.Summary;
        }

        if (!string.IsNullOrEmpty(firstChapter.Language) && !series.Metadata.LanguageLocked)
        {
            series.Metadata.Language = firstChapter.Language;
        }


        void HandleAddPerson(Person person)
        {
            // This first step seems kinda redundant
            PersonHelper.AddPersonIfNotExists(series.Metadata.People, person);
            allPeople.Add(person);
        }

        // Handle People
        foreach (var chapter in chapters)
        {
            if (!series.Metadata.WriterLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Writer).Select(p => p.Name), PersonRole.Writer,
                    HandleAddPerson);
            }

            if (!series.Metadata.CoverArtistLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.CoverArtist).Select(p => p.Name), PersonRole.CoverArtist,
                    HandleAddPerson);
            }

            if (!series.Metadata.PublisherLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Publisher).Select(p => p.Name), PersonRole.Publisher,
                    HandleAddPerson);
            }

            if (!series.Metadata.CharacterLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Character).Select(p => p.Name), PersonRole.Character,
                    HandleAddPerson);
            }

            if (!series.Metadata.ColoristLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Colorist).Select(p => p.Name), PersonRole.Colorist,
                    HandleAddPerson);
            }

            if (!series.Metadata.EditorLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Editor).Select(p => p.Name), PersonRole.Editor,
                    HandleAddPerson);
            }

            if (!series.Metadata.InkerLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Inker).Select(p => p.Name), PersonRole.Inker,
                    HandleAddPerson);
            }

            if (!series.Metadata.LettererLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Letterer).Select(p => p.Name), PersonRole.Letterer,
                    HandleAddPerson);
            }

            if (!series.Metadata.PencillerLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Penciller).Select(p => p.Name), PersonRole.Penciller,
                    HandleAddPerson);
            }

            if (!series.Metadata.TranslatorLocked)
            {
                PersonHelper.UpdatePeople(allPeople, chapter.People.Where(p => p.Role == PersonRole.Translator).Select(p => p.Name), PersonRole.Translator,
                    HandleAddPerson);
            }

            if (!series.Metadata.TagsLocked)
            {
                TagHelper.UpdateTag(allTags, chapter.Tags.Select(t => t.Title), false, (tag, _) =>
                {
                    TagHelper.AddTagIfNotExists(series.Metadata.Tags, tag);
                    allTags.Add(tag);
                });
            }

            if (!series.Metadata.GenresLocked)
            {
                GenreHelper.UpdateGenre(allGenres, chapter.Genres.Select(t => t.Title), false, genre =>
                {
                    GenreHelper.AddGenreIfNotExists(series.Metadata.Genres, genre);
                    allGenres.Add(genre);
                });
            }
        }

        // NOTE: The issue here is that people is just from chapter, but series metadata might already have some people on it
        // I might be able to filter out people that are in locked fields?
        var people = chapters.SelectMany(c => c.People).ToList();
        PersonHelper.KeepOnlySamePeopleBetweenLists(series.Metadata.People,
            people, person =>
            {
                switch (person.Role)
                {
                    case PersonRole.Writer:
                        if (!series.Metadata.WriterLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Penciller:
                        if (!series.Metadata.PencillerLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Inker:
                        if (!series.Metadata.InkerLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Colorist:
                        if (!series.Metadata.ColoristLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Letterer:
                        if (!series.Metadata.LettererLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.CoverArtist:
                        if (!series.Metadata.CoverArtistLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Editor:
                        if (!series.Metadata.EditorLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Publisher:
                        if (!series.Metadata.PublisherLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Character:
                        if (!series.Metadata.CharacterLocked) series.Metadata.People.Remove(person);
                        break;
                    case PersonRole.Translator:
                        if (!series.Metadata.TranslatorLocked) series.Metadata.People.Remove(person);
                        break;
                    default:
                        series.Metadata.People.Remove(person);
                        break;
                }
            });
    }



    private void UpdateVolumes(Series series, IList<ParserInfo> parsedInfos, BlockingCollection<Person> allPeople, BlockingCollection<Tag> allTags, BlockingCollection<Genre> allGenres)
    {
        var startingVolumeCount = series.Volumes.Count;
        // Add new volumes and update chapters per volume
        var distinctVolumes = parsedInfos.DistinctVolumes();
        _logger.LogDebug("[ScannerService] Updating {DistinctVolumes} volumes on {SeriesName}", distinctVolumes.Count, series.Name);
        foreach (var volumeNumber in distinctVolumes)
        {
            var volume = series.Volumes.SingleOrDefault(s => s.Name == volumeNumber);
            if (volume == null)
            {
                volume = DbFactory.Volume(volumeNumber);
                series.Volumes.Add(volume);
                _unitOfWork.VolumeRepository.Add(volume);
            }

            volume.Name = volumeNumber;

            _logger.LogDebug("[ScannerService] Parsing {SeriesName} - Volume {VolumeNumber}", series.Name, volume.Name);
            var infos = parsedInfos.Where(p => p.Volumes == volumeNumber).ToArray();
            UpdateChapters(series, volume, infos);
            volume.Pages = volume.Chapters.Sum(c => c.Pages);

            // Update all the metadata on the Chapters
            foreach (var chapter in volume.Chapters)
            {
                var firstFile = chapter.Files.MinBy(x => x.Chapter);
                if (firstFile == null || _cacheHelper.HasFileNotChangedSinceCreationOrLastScan(chapter, false, firstFile)) continue;
                try
                {
                    var firstChapterInfo = infos.SingleOrDefault(i => i.FullFilePath.Equals(firstFile.FilePath));
                    UpdateChapterFromComicInfo(chapter, allPeople, allTags, allGenres, firstChapterInfo?.ComicInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "There was some issue when updating chapter's metadata");
                }
            }
        }

        // Remove existing volumes that aren't in parsedInfos
        var nonDeletedVolumes = series.Volumes.Where(v => parsedInfos.Select(p => p.Volumes).Contains(v.Name)).ToList();
        if (series.Volumes.Count != nonDeletedVolumes.Count)
        {
            _logger.LogDebug("[ScannerService] Removed {Count} volumes from {SeriesName} where parsed infos were not mapping with volume name",
                (series.Volumes.Count - nonDeletedVolumes.Count), series.Name);
            var deletedVolumes = series.Volumes.Except(nonDeletedVolumes);
            foreach (var volume in deletedVolumes)
            {
                var file = volume.Chapters.FirstOrDefault()?.Files?.FirstOrDefault()?.FilePath ?? "";
                if (!string.IsNullOrEmpty(file) && _directoryService.FileSystem.File.Exists(file))
                {
                    _logger.LogError(
                        "[ScannerService] Volume cleanup code was trying to remove a volume with a file still existing on disk. File: {File}",
                        file);
                }

                _logger.LogDebug("[ScannerService] Removed {SeriesName} - Volume {Volume}: {File}", series.Name, volume.Name, file);
            }

            series.Volumes = nonDeletedVolumes;
        }

        _logger.LogDebug("[ScannerService] Updated {SeriesName} volumes from {StartingVolumeCount} to {VolumeCount}",
            series.Name, startingVolumeCount, series.Volumes.Count);
    }

    private void UpdateChapters(Series series, Volume volume, IList<ParserInfo> parsedInfos)
    {
        // Add new chapters
        foreach (var info in parsedInfos)
        {
            // Specials go into their own chapters with Range being their filename and IsSpecial = True. Non-Specials with Vol and Chap as 0
            // also are treated like specials for UI grouping.
            Chapter chapter;
            try
            {
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
                    "[ScannerService] Adding new chapter, {Series} - Vol {Volume} Ch {Chapter}", info.Series, info.Volumes, info.Chapters);
                chapter = DbFactory.Chapter(info);
                volume.Chapters.Add(chapter);
                series.LastChapterAdded = DateTime.Now;
            }
            else
            {
                chapter.UpdateFrom(info);
            }

            if (chapter == null) continue;
            // Add files
            var specialTreatment = info.IsSpecialInfo();
            AddOrUpdateFileForChapter(chapter, info);
            chapter.Number = Parser.Parser.MinNumberFromRange(info.Chapters) + string.Empty;
            chapter.Range = specialTreatment ? info.Filename : info.Chapters;
        }


        // Remove chapters that aren't in parsedInfos or have no files linked
        var existingChapters = volume.Chapters.ToList();
        foreach (var existingChapter in existingChapters)
        {
            if (existingChapter.Files.Count == 0 || !parsedInfos.HasInfo(existingChapter))
            {
                _logger.LogDebug("[ScannerService] Removed chapter {Chapter} for Volume {VolumeNumber} on {SeriesName}", existingChapter.Range, volume.Name, parsedInfos[0].Series);
                volume.Chapters.Remove(existingChapter);
            }
            else
            {
                // Ensure we remove any files that no longer exist AND order
                existingChapter.Files = existingChapter.Files
                    .Where(f => parsedInfos.Any(p => p.FullFilePath == f.FilePath))
                    .OrderByNatural(f => f.FilePath).ToList();
                existingChapter.Pages = existingChapter.Files.Sum(f => f.Pages);
            }
        }
    }

    private void AddOrUpdateFileForChapter(Chapter chapter, ParserInfo info)
    {
        chapter.Files ??= new List<MangaFile>();
        var existingFile = chapter.Files.SingleOrDefault(f => f.FilePath == info.FullFilePath);
        if (existingFile != null)
        {
            existingFile.Format = info.Format;
            if (!_fileService.HasFileBeenModifiedSince(existingFile.FilePath, existingFile.LastModified) && existingFile.Pages != 0) return;
            existingFile.Pages = _readingItemService.GetNumberOfPages(info.FullFilePath, info.Format);
            // We skip updating DB here with last modified time so that metadata refresh can do it
        }
        else
        {
            var file = DbFactory.MangaFile(info.FullFilePath, info.Format, _readingItemService.GetNumberOfPages(info.FullFilePath, info.Format));
            if (file == null) return;

            chapter.Files.Add(file);
        }
    }

    #nullable enable
    private void UpdateChapterFromComicInfo(Chapter chapter, BlockingCollection<Person> allPeople, BlockingCollection<Tag> allTags, BlockingCollection<Genre> allGenres, ComicInfo? info)
    {
        var firstFile = chapter.Files.MinBy(x => x.Chapter);
        if (firstFile == null ||
            _cacheHelper.HasFileNotChangedSinceCreationOrLastScan(chapter, false, firstFile)) return;

        var comicInfo = info;
        if (info == null)
        {
            comicInfo = _readingItemService.GetComicInfo(firstFile.FilePath);
        }

        if (comicInfo == null) return;
        _logger.LogDebug("[ScannerService] Read ComicInfo for {File}", firstFile.FilePath);

        chapter.AgeRating = ComicInfo.ConvertAgeRatingToEnum(comicInfo.AgeRating);

        if (!string.IsNullOrEmpty(comicInfo.Title))
        {
            chapter.TitleName = comicInfo.Title.Trim();
        }

        if (!string.IsNullOrEmpty(comicInfo.Summary))
        {
            chapter.Summary = comicInfo.Summary;
        }

        if (!string.IsNullOrEmpty(comicInfo.LanguageISO))
        {
            chapter.Language = comicInfo.LanguageISO;
        }

        if (comicInfo.Count > 0)
        {
            chapter.TotalCount = comicInfo.Count;
        }

        // This needs to check against both Number and Volume to calculate Count
        if (!string.IsNullOrEmpty(comicInfo.Number) && float.Parse(comicInfo.Number) > 0)
        {
            chapter.Count = (int) Math.Floor(float.Parse(comicInfo.Number));
        }
        if (!string.IsNullOrEmpty(comicInfo.Volume) && float.Parse(comicInfo.Volume) > 0)
        {
            chapter.Count = Math.Max(chapter.Count, (int) Math.Floor(float.Parse(comicInfo.Volume)));
        }

        void AddPerson(Person person)
        {
            _unitOfWork.PersonRepository.Attach(person);
            _unitOfWork.Commit();
            PersonHelper.AddPersonIfNotExists(chapter.People, person);
        }

        void AddGenre(Genre genre)
        {
            chapter.Genres.Add(genre);
        }

        void AddTag(Tag tag, bool added)
        {
            chapter.Tags.Add(tag);
        }


        if (comicInfo.Year > 0)
        {
            var day = Math.Max(comicInfo.Day, 1);
            var month = Math.Max(comicInfo.Month, 1);
            chapter.ReleaseDate = DateTime.Parse($"{month}/{day}/{comicInfo.Year}");
        }

        var people = GetTagValues(comicInfo.Colorist);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Colorist);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Colorist,
            AddPerson);

        people = GetTagValues(comicInfo.Characters);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Character);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Character,
            AddPerson);


        people = GetTagValues(comicInfo.Translator);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Translator);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Translator,
            AddPerson);


        people = GetTagValues(comicInfo.Writer);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Writer);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Writer,
            AddPerson);

        people = GetTagValues(comicInfo.Editor);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Editor);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Editor,
            AddPerson);

        people = GetTagValues(comicInfo.Inker);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Inker);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Inker,
            AddPerson);

        people = GetTagValues(comicInfo.Letterer);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Letterer);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Letterer,
            AddPerson);


        people = GetTagValues(comicInfo.Penciller);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Penciller);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Penciller,
            AddPerson);

        people = GetTagValues(comicInfo.CoverArtist);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.CoverArtist);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.CoverArtist,
            AddPerson);

        people = GetTagValues(comicInfo.Publisher);
        PersonHelper.RemovePeople(chapter.People, people, PersonRole.Publisher);
        PersonHelper.UpdatePeople(allPeople, people, PersonRole.Publisher,
            AddPerson);

        var genres = GetTagValues(comicInfo.Genre);
        GenreHelper.KeepOnlySameGenreBetweenLists(chapter.Genres, genres.Select(g => DbFactory.Genre(g, false)).ToList());
        GenreHelper.UpdateGenre(allGenres, genres, false,
            AddGenre);

        var tags = GetTagValues(comicInfo.Tags);
        TagHelper.KeepOnlySameTagBetweenLists(chapter.Tags, tags.Select(t => DbFactory.Tag(t, false)).ToList());
        TagHelper.UpdateTag(allTags, tags, false,
            AddTag);
    }

    private static IList<string> GetTagValues(string comicInfoTagSeparatedByComma)
    {

        if (!string.IsNullOrEmpty(comicInfoTagSeparatedByComma))
        {
            return comicInfoTagSeparatedByComma.Split(",").Select(s => s.Trim()).ToList();
        }
        return ImmutableList<string>.Empty;
    }
    #nullable disable

}
