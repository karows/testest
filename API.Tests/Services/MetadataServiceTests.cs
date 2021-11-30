﻿using System;
using System.IO;
using API.Entities;
using API.Services;
using Xunit;

namespace API.Tests.Services
{
    public class MetadataServiceTests
    {
        private readonly string _testDirectory = Path.Join(Directory.GetCurrentDirectory(), "../../../Services/Test Data/ArchiveService/Archives");
        private const string TestCoverImageFile = "thumbnail.jpg";
        private readonly string _testCoverImageDirectory = Path.Join(Directory.GetCurrentDirectory(), @"../../../Services/Test Data/ArchiveService/CoverImages");
        //private readonly MetadataService _metadataService;
        // private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        // private readonly IImageService _imageService = Substitute.For<IImageService>();
        // private readonly IBookService _bookService = Substitute.For<IBookService>();
        // private readonly IArchiveService _archiveService = Substitute.For<IArchiveService>();
        // private readonly ILogger<MetadataService> _logger = Substitute.For<ILogger<MetadataService>>();
        // private readonly IHubContext<MessageHub> _messageHub = Substitute.For<IHubContext<MessageHub>>();

        public MetadataServiceTests()
        {
            //_metadataService = new MetadataService(_unitOfWork, _logger, _archiveService, _bookService, _imageService, _messageHub);
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnFirstRun()
        {
            // Represents first run
            Assert.True(MetadataService.ShouldUpdateCoverImage(null, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = DateTime.Now
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)), false, false));
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnFirstRunSeries()
        {
            // Represents first run
            Assert.True(MetadataService.ShouldUpdateCoverImage(null,null, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, false));
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnFirstRun_FileModified()
        {
            // Represents first run
            Assert.True(MetadataService.ShouldUpdateCoverImage(null, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = new FileInfo(Path.Join(_testDirectory, "file in folder.zip")).LastWriteTime.Subtract(TimeSpan.FromDays(1))
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, false));
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnFirstRun_CoverImageLocked()
        {
            // Represents first run
            Assert.True(MetadataService.ShouldUpdateCoverImage(null, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = new FileInfo(Path.Join(_testDirectory, "file in folder.zip")).LastWriteTime
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, true));
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnSecondRun_ForceUpdate()
        {
            // Represents first run
            Assert.True(MetadataService.ShouldUpdateCoverImage(null, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = new FileInfo(Path.Join(_testDirectory, "file in folder.zip")).LastWriteTime
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),true, false));
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnSecondRun_NoFileChangeButNoCoverImage()
        {
            // Represents first run
            Assert.True(MetadataService.ShouldUpdateCoverImage(null, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = new FileInfo(Path.Join(_testDirectory, "file in folder.zip")).LastWriteTime
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, false));
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnSecondRun_FileChangeButNoCoverImage()
        {
            // Represents first run
            Assert.True(MetadataService.ShouldUpdateCoverImage(null, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = new FileInfo(Path.Join(_testDirectory, "file in folder.zip")).LastWriteTime + TimeSpan.FromDays(1)
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, false));
        }

        [Fact]
        public void ShouldNotUpdateCoverImage_OnSecondRun_CoverImageSet()
        {
            // Represents first run
            Assert.False(MetadataService.ShouldUpdateCoverImage(TestCoverImageFile, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = new FileInfo(Path.Join(_testDirectory, "file in folder.zip")).LastWriteTime
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, false, _testCoverImageDirectory));
        }

        [Fact]
        public void ShouldNotUpdateCoverImage_OnSecondRun_HasCoverImage_NoForceUpdate_NoLock()
        {

            Assert.False(MetadataService.ShouldUpdateCoverImage(TestCoverImageFile, new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = DateTime.Now
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, false, _testCoverImageDirectory));
        }

        [Fact]
        public void ShouldUpdateCoverImage_OnSecondRun_HasCoverImage_NoForceUpdate_HasLock_CoverImageDoesntExist()
        {

            Assert.True(MetadataService.ShouldUpdateCoverImage(@"doesn't_exist.jpg", new MangaFile()
            {
                FilePath = Path.Join(_testDirectory, "file in folder.zip"),
                LastModified = DateTime.Now
            }, DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),false, true, _testCoverImageDirectory));
        }
    }
}
