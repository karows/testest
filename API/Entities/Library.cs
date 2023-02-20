﻿using System;
using System.Collections.Generic;
using API.Entities.Enums;
using API.Entities.Interfaces;

namespace API.Entities;

public class Library : IEntityDate
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string CoverImage { get; set; }
    public LibraryType Type { get; set; }
    /// <summary>
    /// If Folder Watching is enabled for this library
    /// </summary>
    public bool FolderWatching { get; set; } = true;
    /// <summary>
    /// Include Library series on Dashboard Streams
    /// </summary>
    public bool IncludeInDashboard { get; set; } = true;
    /// <summary>
    /// Include Library series on Recommended Streams
    /// </summary>
    public bool IncludeInRecommended { get; set; } = true;
    /// <summary>
    /// Include library series in Search
    /// </summary>
    public bool IncludeInSearch { get; set; } = true;
    /// <summary>
    /// Should this library create and manage collections from Metadata
    /// </summary>
    public bool ManageCollections { get; set; } = true;
    /// <summary>
    /// When showing series, only parent series or series with no relationships will be returned
    /// </summary>
    public bool CollapseSeriesRelationships { get; set; } = false;
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// Last time Library was scanned
    /// </summary>
    /// <remarks>Time stored in UTC</remarks>
    public DateTime LastScanned { get; set; }
    public ICollection<FolderPath> Folders { get; set; }
    public ICollection<AppUser> AppUsers { get; set; }
    public ICollection<Series> Series { get; set; }

    public void UpdateLastModified()
    {
        LastModified = DateTime.Now;
        LastModifiedUtc = DateTime.UtcNow;
    }

    public void UpdateLastScanned(DateTime? time)
    {
        if (time == null)
        {
            LastScanned = DateTime.Now;
        }
        else
        {
            LastScanned = (DateTime) time;
        }
    }
}
