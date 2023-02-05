﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.Comparators;
using API.Data;
using API.Data.Repositories;
using API.DTOs.ReadingLists;
using API.DTOs.ReadingLists.CBL;
using API.Entities;
using API.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace API.Services;

public interface IReadingListService
{
    Task<bool> RemoveFullyReadItems(int readingListId, AppUser user);
    Task<bool> UpdateReadingListItemPosition(UpdateReadingListPosition dto);
    Task<bool> DeleteReadingListItem(UpdateReadingListPosition dto);
    Task<AppUser?> UserHasReadingListAccess(int readingListId, string username);
    Task<bool> DeleteReadingList(int readingListId, AppUser user);
    Task CalculateReadingListAgeRating(ReadingList readingList);
    Task<bool> AddChaptersToReadingList(int seriesId, IList<int> chapterIds,
        ReadingList readingList);

    Task<CblImportSummaryDto> CreateReadingListFromCbl(int userId, CblReadingList cblReading);
}

/// <summary>
/// Methods responsible for management of Reading Lists
/// </summary>
/// <remarks>If called from API layer, expected for <see cref="UserHasReadingListAccess"/> to be called beforehand</remarks>
public class ReadingListService : IReadingListService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReadingListService> _logger;
    private readonly ChapterSortComparerZeroFirst _chapterSortComparerForInChapterSorting = new ChapterSortComparerZeroFirst();
    private static readonly Regex JustNumbers = new Regex(@"^\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase,
        Tasks.Scanner.Parser.Parser.RegexTimeout);

    public ReadingListService(IUnitOfWork unitOfWork, ILogger<ReadingListService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public static string FormatTitle(ReadingListItemDto item)
    {
        var title = string.Empty;
        if (item.ChapterNumber == Tasks.Scanner.Parser.Parser.DefaultChapter && item.VolumeNumber != Tasks.Scanner.Parser.Parser.DefaultVolume) {
            title = $"Volume {item.VolumeNumber}";
        }

        if (item.SeriesFormat == MangaFormat.Epub) {
            var specialTitle = Tasks.Scanner.Parser.Parser.CleanSpecialTitle(item.ChapterNumber);
            if (specialTitle == Tasks.Scanner.Parser.Parser.DefaultChapter)
            {
                if (!string.IsNullOrEmpty(item.ChapterTitleName))
                {
                    title = item.ChapterTitleName;
                }
                else
                {
                    title = $"Volume {Tasks.Scanner.Parser.Parser.CleanSpecialTitle(item.VolumeNumber)}";
                }
            } else {
                title = $"Volume {specialTitle}";
            }
        }

        var chapterNum = item.ChapterNumber;
        if (!string.IsNullOrEmpty(chapterNum) && !JustNumbers.Match(item.ChapterNumber).Success) {
            chapterNum = Tasks.Scanner.Parser.Parser.CleanSpecialTitle(item.ChapterNumber);
        }

        if (title != string.Empty) return title;

        if (item.ChapterNumber == Tasks.Scanner.Parser.Parser.DefaultChapter &&
            !string.IsNullOrEmpty(item.ChapterTitleName))
        {
            title = item.ChapterTitleName;
        }
        else
        {
            title = ReaderService.FormatChapterName(item.LibraryType, true, true) + chapterNum;
        }
        return title;
    }


    /// <summary>
    /// Removes all entries that are fully read from the reading list. This commits
    /// </summary>
    /// <remarks>If called from API layer, expected for <see cref="UserHasReadingListAccess"/> to be called beforehand</remarks>
    /// <param name="readingListId">Reading List Id</param>
    /// <param name="user">User</param>
    /// <returns></returns>
    public async Task<bool> RemoveFullyReadItems(int readingListId, AppUser user)
    {
        var items = await _unitOfWork.ReadingListRepository.GetReadingListItemDtosByIdAsync(readingListId, user.Id);
        items = await _unitOfWork.ReadingListRepository.AddReadingProgressModifiers(user.Id, items.ToList());

        // Collect all Ids to remove
        var itemIdsToRemove = items.Where(item => item.PagesRead == item.PagesTotal).Select(item => item.Id);

        try
        {
            var listItems =
                (await _unitOfWork.ReadingListRepository.GetReadingListItemsByIdAsync(readingListId)).Where(r =>
                    itemIdsToRemove.Contains(r.Id));
            _unitOfWork.ReadingListRepository.BulkRemove(listItems);

            var readingList = await _unitOfWork.ReadingListRepository.GetReadingListByIdAsync(readingListId);
            await CalculateReadingListAgeRating(readingList);

            if (!_unitOfWork.HasChanges()) return true;

            return await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
        }

        return false;
    }

    /// <summary>
    /// Updates a reading list item from one position to another. This will cause items at that position to be pushed one index.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<bool> UpdateReadingListItemPosition(UpdateReadingListPosition dto)
    {
        var items = (await _unitOfWork.ReadingListRepository.GetReadingListItemsByIdAsync(dto.ReadingListId)).ToList();
        var item = items.Find(r => r.Id == dto.ReadingListItemId);
        items.Remove(item);
        items.Insert(dto.ToPosition, item);

        for (var i = 0; i < items.Count; i++)
        {
            items[i].Order = i;
        }

        if (!_unitOfWork.HasChanges()) return true;

        return await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Removes a certain reading list item from a reading list
    /// </summary>
    /// <param name="dto">Only ReadingListId and ReadingListItemId are used</param>
    /// <returns></returns>
    public async Task<bool> DeleteReadingListItem(UpdateReadingListPosition dto)
    {
        var readingList = await _unitOfWork.ReadingListRepository.GetReadingListByIdAsync(dto.ReadingListId);
        readingList.Items = readingList.Items.Where(r => r.Id != dto.ReadingListItemId).OrderBy(r => r.Order).ToList();

        var index = 0;
        foreach (var readingListItem in readingList.Items)
        {
            readingListItem.Order = index;
            index++;
        }

        await CalculateReadingListAgeRating(readingList);

        if (!_unitOfWork.HasChanges()) return true;

        return await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Calculates the highest Age Rating from each Reading List Item
    /// </summary>
    /// <param name="readingList"></param>
    public async Task CalculateReadingListAgeRating(ReadingList readingList)
    {
        await CalculateReadingListAgeRating(readingList, readingList.Items.Select(i => i.SeriesId));
    }

    /// <summary>
    /// Calculates the highest Age Rating from each Reading List Item
    /// </summary>
    /// <remarks>This method is used when the ReadingList doesn't have items yet</remarks>
    /// <param name="readingList"></param>
    /// <param name="seriesIds">The series ids of all the reading list items</param>
    private async Task CalculateReadingListAgeRating(ReadingList readingList, IEnumerable<int> seriesIds)
    {
        var ageRating = await _unitOfWork.SeriesRepository.GetMaxAgeRatingFromSeriesAsync(seriesIds);
        readingList.AgeRating = ageRating;
    }

    /// <summary>
    /// Validates the user has access to the reading list to perform actions on it
    /// </summary>
    /// <param name="readingListId"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    public async Task<AppUser?> UserHasReadingListAccess(int readingListId, string username)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username,
            AppUserIncludes.ReadingListsWithItems);
        if (user.ReadingLists.SingleOrDefault(rl => rl.Id == readingListId) == null && !await _unitOfWork.UserRepository.IsUserAdminAsync(user))
        {
            return null;
        }

        return user;
    }

    /// <summary>
    /// Removes the Reading List from kavita
    /// </summary>
    /// <param name="readingListId"></param>
    /// <param name="user">User should have ReadingLists populated</param>
    /// <returns></returns>
    public async Task<bool> DeleteReadingList(int readingListId, AppUser user)
    {
        var readingList = await _unitOfWork.ReadingListRepository.GetReadingListByIdAsync(readingListId);
        user.ReadingLists.Remove(readingList);

        if (!_unitOfWork.HasChanges()) return true;

        return await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Adds a list of Chapters as reading list items to the passed reading list.
    /// </summary>
    /// <param name="seriesId"></param>
    /// <param name="chapterIds"></param>
    /// <param name="readingList"></param>
    /// <returns>True if new chapters were added</returns>
    public async Task<bool> AddChaptersToReadingList(int seriesId, IList<int> chapterIds, ReadingList readingList)
    {
        readingList.Items ??= new List<ReadingListItem>();
        var lastOrder = 0;
        if (readingList.Items.Any())
        {
            lastOrder = readingList.Items.DefaultIfEmpty().Max(rli => rli.Order);
        }

        var existingChapterExists = readingList.Items.Select(rli => rli.ChapterId).ToHashSet();
        var chaptersForSeries = (await _unitOfWork.ChapterRepository.GetChaptersByIdsAsync(chapterIds, ChapterIncludes.Volumes))
            .OrderBy(c => Tasks.Scanner.Parser.Parser.MinNumberFromRange(c.Volume.Name))
            .ThenBy(x => double.Parse(x.Number), _chapterSortComparerForInChapterSorting)
            .ToList();

        var index = lastOrder + 1;
        foreach (var chapter in chaptersForSeries.Where(chapter => !existingChapterExists.Contains(chapter.Id)))
        {
            readingList.Items.Add(DbFactory.ReadingListItem(index, seriesId, chapter.VolumeId, chapter.Id));
            index += 1;
        }

        await CalculateReadingListAgeRating(readingList, new []{ seriesId });

        return index > lastOrder + 1;
    }

    public async Task<CblImportSummaryDto> CreateReadingListFromCbl(int userId, CblReadingList cblReading)
    {
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(userId, AppUserIncludes.ReadingLists);
        _logger.LogDebug("Importing {ReadingListName} CBL for User {UserName}", cblReading.Name, user.UserName);
        var importSummary = new CblImportSummaryDto()
        {
            CblName = cblReading.Name,
            Success = CblImportResult.Success,
            Results = new List<CblBookResult>()
        };

        if (cblReading.Books == null || cblReading.Books.Book.Count == 0)
        {
            importSummary.Results.Add(new CblBookResult()
            {
                Reason = "CBL is empty"
            });
            importSummary.Success = CblImportResult.Fail;
            return importSummary;
        }

        var uniqueSeries = cblReading.Books.Book.Select(b => Tasks.Scanner.Parser.Parser.Normalize(b.Series)).Distinct();
        // TODO: Make this user id/restriction based
        var allSeries =
            (await _unitOfWork.SeriesRepository.GetAllSeriesByNameAsync(uniqueSeries, SeriesIncludes.Chapters)).ToDictionary(s => s.NormalizedName);

        if (allSeries.Count == 0)
        {
            // Report that no series exist in the reading list
            importSummary.Results.Add(new CblBookResult()
            {
                Reason = "CBL contains no series that exist within Kavita or user doesn't have access to this Series"
            });
            importSummary.Success = CblImportResult.Fail;
            return importSummary;
        }

        var readingListNameNormalized = Tasks.Scanner.Parser.Parser.Normalize(cblReading.Name);
        var allReadingLists = (await _unitOfWork.ReadingListRepository.GetAllReadingListsAsync()).ToDictionary(s => s.NormalizedTitle);
        if (!allReadingLists.TryGetValue(readingListNameNormalized, out var readingList))
        {
            readingList = DbFactory.ReadingList(cblReading.Name, string.Empty, false);
            user.ReadingLists.Add(readingList);
        }
        else
        {
            // Reading List exists, check if we own it
            if (user.ReadingLists.All(l => l.NormalizedTitle != readingListNameNormalized))
            {
                importSummary.Results.Add(new CblBookResult()
                {
                    Reason = "CBL name conflicts with another Reading List within Kavita that user does not own"
                });
                importSummary.Success = CblImportResult.Fail;
                return importSummary;
            }
        }

        readingList.Items ??= new List<ReadingListItem>();
        var successfulProcessedItem = 0;
        foreach (var (book, i) in cblReading.Books.Book.Select((value, i) => ( value, i )))
        {
            var normalizedSeries = Tasks.Scanner.Parser.Parser.Normalize(book.Series);
            if (!allSeries.TryGetValue(normalizedSeries, out var bookSeries))
            {
                importSummary.Results.Add(new CblBookResult()
                {
                    Series = book.Series,
                    Volume = book.Volume,
                    Number = book.Number,
                    Reason = "Series could not be found"
                });
                continue; // Report failure
            }
            // Prioritize lookup by Volume then Chapter, but allow fallback to just Chapter
            var matchingVolume = bookSeries.Volumes.FirstOrDefault(v => book.Volume == v.Name) ?? bookSeries.Volumes.FirstOrDefault(v => v.Number == 0);
            if (matchingVolume == null)
            {
                importSummary.Results.Add(new CblBookResult()
                {
                    Series = book.Series,
                    Volume = book.Volume,
                    Number = book.Number,
                    Reason = "Volume Could not be found (or Volume not present and could not find individual chapter/issue"
                });
                continue;
            }

            var chapter = matchingVolume.Chapters.FirstOrDefault(c => c.Number == book.Number);
            if (chapter == null)
            {
                importSummary.Results.Add(new CblBookResult()
                {
                    Series = book.Series,
                    Volume = book.Volume,
                    Number = book.Number,
                    Reason = "Chapter/Issue could not be found"
                });
                continue;
            }


            // See if a matching item already exists
            var readingListItem =
                readingList.Items.FirstOrDefault(item =>
                    item.SeriesId == bookSeries.Id && item.ChapterId == chapter.Id);
            if (readingListItem == null)
            {
                readingListItem = DbFactory.ReadingListItem(readingList.Items.Count, bookSeries.Id,
                    matchingVolume.Id, chapter.Id);
                readingList.Items.Add(readingListItem);
            }
            successfulProcessedItem++;
        }

        if (successfulProcessedItem != cblReading.Books.Book.Count || importSummary.Results.Count > 0)
        {
            importSummary.Success = CblImportResult.Partial;
        }

        await CalculateReadingListAgeRating(readingList);

        if (!_unitOfWork.HasChanges()) return importSummary;
        await _unitOfWork.CommitAsync();


        return importSummary;
    }
}
