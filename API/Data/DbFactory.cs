﻿using System.Collections.Generic;
using System.IO;
using API.Entities;
using API.Entities.Enums;
using API.Entities.Metadata;
using API.Extensions;
using API.Parser;
using API.Services.Tasks;
using Kavita.Common;

namespace API.Data;

/// <summary>
/// Responsible for creating Series, Volume, Chapter, MangaFiles for use in <see cref="ScannerService"/>
/// </summary>
public static class DbFactory
{
    public static Chapter Chapter(ParserInfo info)
    {
        var specialTreatment = info.IsSpecialInfo();
        var specialTitle = specialTreatment ? info.Filename : info.Chapters;
        return new Chapter()
        {
            Number = specialTreatment ? Services.Tasks.Scanner.Parser.Parser.DefaultChapter : Services.Tasks.Scanner.Parser.Parser.MinNumberFromRange(info.Chapters) + string.Empty,
            Range = specialTreatment ? info.Filename : info.Chapters,
            Title = (specialTreatment && info.Format == MangaFormat.Epub)
                ? info.Title
                : specialTitle,
            Files = new List<MangaFile>(),
            IsSpecial = specialTreatment,
        };
    }


    public static ReadingListItem ReadingListItem(int index, int seriesId, int volumeId, int chapterId)
    {
        return new ReadingListItem()
        {
            Order = index,
            ChapterId = chapterId,
            SeriesId = seriesId,
            VolumeId = volumeId
        };
    }
}
