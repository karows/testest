﻿using System;
using API.DTOs.Update;
using API.Entities;

namespace API.SignalR
{
    public static class MessageFactory
    {
        public static SignalRMessage ScanSeriesEvent(int seriesId, string seriesName)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.ScanSeries,
                Body = new
                {
                    SeriesId = seriesId,
                    SeriesName = seriesName
                }
            };
        }

        public static SignalRMessage SeriesAddedEvent(int seriesId, string seriesName, int libraryId)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.SeriesAdded,
                Body = new
                {
                    SeriesId = seriesId,
                    SeriesName = seriesName,
                    LibraryId = libraryId
                }
            };
        }

        public static SignalRMessage SeriesRemovedEvent(int seriesId, string seriesName, int libraryId)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.SeriesRemoved,
                Body = new
                {
                    SeriesId = seriesId,
                    SeriesName = seriesName,
                    LibraryId = libraryId
                }
            };
        }

        public static SignalRMessage ScanLibraryProgressEvent(int libraryId, float progress)
        {
            // TODO: Remove this?
            return new SignalRMessage()
            {
                Name = SignalREvents.ScanLibraryProgress,
                Title = $"Scanning {libraryId}",
                Body = new
                {
                    LibraryId = libraryId,
                    Progress = progress,
                    EventTime = DateTime.Now
                }
            };
        }

        public static SignalRMessage RefreshMetadataProgressEvent(int libraryId, float progress)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.RefreshMetadataProgress,
                Title = "Refreshing Cover Images",
                Body = new
                {
                    LibraryId = libraryId,
                    Progress = progress,
                    EventTime = DateTime.Now
                }
            };
        }



        public static SignalRMessage RefreshMetadataEvent(int libraryId, int seriesId)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.RefreshMetadata,
                Title = "Refreshing Cover Images", // This doesn't need a title, it doesn't display on the UI widget
                Body = new
                {
                    SeriesId = seriesId,
                    LibraryId = libraryId
                }
            };
        }

        public static SignalRMessage BackupDatabaseProgressEvent(float progress)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.BackupDatabaseProgress,
                Title = "Backing up Database",
                Body = new
                {
                    Progress = progress
                }
            };
        }
        public static SignalRMessage CleanupProgressEvent(float progress)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.CleanupProgress,
                Title = "Cleaning up Server", // TODO: Find a better word for this
                Body = new
                {
                    Progress = progress
                }
            };
        }


        public static SignalRMessage UpdateVersionEvent(UpdateNotificationDto update)
        {
            return new SignalRMessage
            {
                Name = SignalREvents.UpdateAvailable,
                Title = "Update Available",
                Body = update
            };
        }

        public static SignalRMessage SeriesAddedToCollection(int tagId, int seriesId)
        {
            return new SignalRMessage
            {
                Name = SignalREvents.SeriesAddedToCollection,
                Body = new
                {
                    TagId = tagId,
                    SeriesId = seriesId
                }
            };
        }

        public static SignalRMessage ScanLibraryError(int libraryId, string libraryName)
        {
            return new SignalRMessage
            {
                Name = SignalREvents.ScanLibraryError,
                Title = "Error",
                SubTitle = $"Error Scanning {libraryName}",
                Body = new
                {
                    LibraryId = libraryId,
                }
            };
        }

        public static SignalRMessage DownloadProgressEvent(string username, string downloadName, float progress)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.DownloadProgress,
                Title = $"Downloading {downloadName}",
                SubTitle = $"{username} is downloading",
                Body = new
                {
                    UserName = username,
                    DownloadName = downloadName,
                    Progress = progress
                }
            };
        }


        public static SignalRMessage FileScanProgressEvent(string filename, string libraryName, string eventType)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.FileScanProgress,
                Title = $"Scanning {libraryName}",
                SubTitle = filename,
                EventType = eventType,
                Body = new
                {
                    Title = $"Scanning {libraryName}",
                    Subtitle = filename,
                    EventTime = DateTime.Now
                }
            };
        }

        public static SignalRMessage DbUpdateProgressEvent(Series series, string eventType)
        {
            return new SignalRMessage()
            {
                Name = SignalREvents.ScanSeries,
                Title = "Updating Series",
                SubTitle = series.Name,
                EventType = eventType,
                Body = new
                {
                    Title = "Updating Series",
                    SubTitle = series.Name
                }
            };
        }
    }
}
