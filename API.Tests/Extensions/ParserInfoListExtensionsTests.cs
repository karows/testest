﻿using System.Collections.Generic;
using System.Linq;
using API.Entities;
using API.Entities.Enums;
using API.Extensions;
using API.Parser;
using API.Tests.Helpers;
using Xunit;

namespace API.Tests.Extensions
{
    public class ParserInfoListExtensions
    {
        [Theory]
        [InlineData(new string[] {"1", "1", "3-5", "5", "8", "0", "0"}, new string[] {"1", "3-5", "5", "8", "0"})]
        public void DistinctVolumesTest(string[] volumeNumbers, string[] expectedNumbers)
        {
            var infos = volumeNumbers.Select(n => new ParserInfo() {Volumes = n}).ToList();
            Assert.Equal(expectedNumbers, infos.DistinctVolumes());
        }
        
        [Theory]
        [InlineData(new string[] {@"Cynthia The Mission - c000-006 (v06) [Desudesu&Brolen].zip"}, new string[] {})]
        [InlineData(new string[] {@"Cynthia The Mission - c000-006 (v06-07) [Desudesu&Brolen].zip"}, new string[] {})]
        [InlineData(new string[] {@"Cynthia The Mission [Desudesu&Brolen].zip"}, new string[] {})]
        public void HasInfoTest(string[] inputInfos, string[] inputChapters)
        {
            var infos = new List<ParserInfo>();
            foreach (var filename in inputInfos)
            {
                infos.Add(API.Parser.Parser.Parse(
                    filename,
                    string.Empty));
            }

            // I need a simple way to generate chapters that is based on real code, not these simple mocks. 
            var chapter = EntityFactory.CreateChapter("0-6", false, new List<MangaFile>()
            {
                EntityFactory.CreateMangaFile(@"E:\Manga\Cynthia the Mission\Cynthia The Mission - c000-006 (v06) [Desudesu&Brolen].zip", MangaFormat.Archive, 199)
            });

            Assert.True(infos.HasInfo(chapter));
        }
    }
}