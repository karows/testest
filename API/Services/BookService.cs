﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.Entities.Enums;
using API.Interfaces;
using API.Parser;
using ExCSS;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetVips;
using VersOne.Epub;

namespace API.Services
{
    public class BookService : IBookService
    {
        private readonly ILogger<BookService> _logger;

        private const int ThumbnailWidth = 320; // 153w x 230h
        private readonly StylesheetParser _cssParser = new ();
      
        public BookService(ILogger<BookService> logger)
        {
            _logger = logger;
        }
        
        private static bool HasClickableHrefPart(HtmlNode anchor)
        {
            return anchor.GetAttributeValue("href", string.Empty).Contains("#") 
                   && anchor.GetAttributeValue("tabindex", string.Empty) != "-1"
                   && anchor.GetAttributeValue("role", string.Empty) != "presentation";
        }

        public static string GetContentType(EpubContentType type)
        {
            string contentType;
            switch (type)
            {
                case EpubContentType.IMAGE_GIF:
                    contentType = "image/gif";
                    break;
                case EpubContentType.IMAGE_PNG:
                    contentType = "image/png";
                    break;
                case EpubContentType.IMAGE_JPEG:
                    contentType = "image/jpeg";
                    break;
                case EpubContentType.FONT_OPENTYPE:
                    contentType = "font/otf";
                    break;
                case EpubContentType.FONT_TRUETYPE:
                    contentType = "font/ttf";
                    break;
                case EpubContentType.IMAGE_SVG:
                    contentType = "image/svg+xml";
                    break;
                default:
                    contentType = "application/octet-stream";
                    break;
            }

            return contentType;
        }

        public static void UpdateLinks(HtmlNode anchor, Dictionary<string, int> mappings, int currentPage)
        {
            if (anchor.Name != "a") return;
            var hrefParts = BookService.CleanContentKeys(anchor.GetAttributeValue("href", string.Empty))
                .Split("#");
            var mappingKey = hrefParts[0];
            if (!mappings.ContainsKey(mappingKey))
            {
                if (HasClickableHrefPart(anchor))
                {
                    var part = hrefParts.Length > 1
                        ? hrefParts[1]
                        : anchor.GetAttributeValue("href", string.Empty);
                    anchor.Attributes.Add("kavita-page", $"{currentPage}");
                    anchor.Attributes.Add("kavita-part", part);
                    anchor.Attributes.Remove("href");
                    anchor.Attributes.Add("href", "javascript:void(0)");
                }
                else
                {
                    anchor.Attributes.Add("target", "_blank");    
                }

                return;
            }
                                
            var mappedPage = mappings[mappingKey];
            anchor.Attributes.Add("kavita-page", $"{mappedPage}");
            if (hrefParts.Length > 1)
            {
                anchor.Attributes.Add("kavita-part",
                    hrefParts[1]);
            }
                            
            anchor.Attributes.Remove("href");
            anchor.Attributes.Add("href", "javascript:void(0)");
        }

        public async Task<string> ScopeStyles(string stylesheetHtml, string apiBase, string filename, EpubBookRef book)
        {
            // @Import statements will be handled by browser, so we must inline the css into the original file that request it, so they can be
            // Scoped
            var prepend = filename.Length > 0 ? filename.Replace(Path.GetFileName(filename), "") : string.Empty;
            var importBuilder = new StringBuilder();
            foreach (Match match in Parser.Parser.CssImportUrlRegex.Matches(stylesheetHtml))
            {
                if (!match.Success) continue;
                
                var importFile = match.Groups["Filename"].Value;
                var key = CleanContentKeys(importFile);
                if (!key.Contains(prepend))
                {
                    key = prepend + key;
                }
                if (!book.Content.AllFiles.ContainsKey(key)) continue;
            
                var bookFile = book.Content.AllFiles[key];
               var content = await bookFile.ReadContentAsBytesAsync();
               importBuilder.Append(Encoding.UTF8.GetString(content));
            }

            stylesheetHtml = stylesheetHtml.Insert(0, importBuilder.ToString());
            stylesheetHtml =
                Parser.Parser.CssImportUrlRegex.Replace(stylesheetHtml, "$1" + apiBase + prepend + "$2" + "$3");
            
            var styleContent = RemoveWhiteSpaceFromStylesheets(stylesheetHtml);
            styleContent =
                Parser.Parser.FontSrcUrlRegex.Replace(styleContent, "$1" + apiBase + "$2" + "$3");

            styleContent = styleContent.Replace("body", ".reading-section");
            
            var stylesheet = await _cssParser.ParseAsync(styleContent);
            foreach (var styleRule in stylesheet.StyleRules)
            {
                if (styleRule.Selector.Text == ".reading-section") continue;
                if (styleRule.Selector.Text.Contains(","))
                {
                    styleRule.Text = styleRule.Text.Replace(styleRule.SelectorText,
                        string.Join(", ",
                            styleRule.Selector.Text.Split(",").Select(s => ".reading-section " + s)));
                    continue;
                }
                styleRule.Text = ".reading-section " + styleRule.Text;
            }
            return RemoveWhiteSpaceFromStylesheets(stylesheet.ToCss());
        }

        public string GetSummaryInfo(string filePath)
        {
            if (!IsValidFile(filePath)) return string.Empty;

            try
            {
                using var epubBook = EpubReader.OpenBook(filePath);
                return epubBook.Schema.Package.Metadata.Description;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BookService] There was an exception getting summary, defaulting to empty string");
            }

            return string.Empty;
        }

        private bool IsValidFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("[BookService] Book {EpubFile} could not be found", filePath);
                return false;
            }

            if (Parser.Parser.IsBook(filePath)) return true;
            
            _logger.LogError("[BookService] Book {EpubFile} is not a valid EPUB", filePath);
            return false; 
        }

        public int GetNumberOfPages(string filePath)
        {
            if (!IsValidFile(filePath) || !Parser.Parser.IsEpub(filePath)) return 0;

            try
            {
                using var epubBook = EpubReader.OpenBook(filePath);
                return epubBook.Content.Html.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BookService] There was an exception getting number of pages, defaulting to 0");
            }

            return 0;
        }

        public static string CleanContentKeys(string key)
        {
            return key.Replace("../", string.Empty);
        }

        public async Task<Dictionary<string, int>> CreateKeyToPageMappingAsync(EpubBookRef book)
        {
            var dict = new Dictionary<string, int>();
            var pageCount = 0;
            foreach (var contentFileRef in await book.GetReadingOrderAsync())
            {
                if (contentFileRef.ContentType != EpubContentType.XHTML_1_1) continue;
                dict.Add(contentFileRef.FileName, pageCount);
                pageCount += 1;
            }
            
            return dict;
        }

        /// <summary>
        /// Parses out Title from book. Chapters and Volumes will always be "0". If there is any exception reading book (malformed books)
        /// then null is returned.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public ParserInfo ParseInfo(string filePath)
        {
            try
            {
                using var epubBook = EpubReader.OpenBook(filePath);

                return new ParserInfo()
                {
                    Chapters = "0",
                    Edition = "",
                    Format = MangaFormat.Book,
                    Filename = Path.GetFileName(filePath),
                    Title = epubBook.Title,
                    FullFilePath = filePath,
                    IsSpecial = false,
                    Series = epubBook.Title,
                    Volumes = "0"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BookService] There was an exception when opening epub book: {FileName}", filePath);
            }

            return null;
        }
        

        public byte[] GetCoverImage(string fileFilePath, bool createThumbnail = true)
        {
            if (!IsValidFile(fileFilePath)) return Array.Empty<byte>();
            
            using var epubBook = EpubReader.OpenBook(fileFilePath);


            try
            {
                // Try to get the cover image from OPF file, if not set, try to parse it from all the files, then result to the first one.
                var coverImageContent = epubBook.Content.Cover
                                        ?? epubBook.Content.Images.Values.FirstOrDefault(file => Parser.Parser.IsCoverImage(file.FileName))
                                        ?? epubBook.Content.Images.Values.First();
                
                if (coverImageContent == null) return Array.Empty<byte>();

                if (createThumbnail)
                {
                    using var stream = new MemoryStream(coverImageContent.ReadContent());

                    using var thumbnail = Image.ThumbnailStream(stream, ThumbnailWidth);
                    return thumbnail.WriteToBuffer(".jpg");
                }

                return coverImageContent.ReadContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BookService] There was a critical error and prevented thumbnail generation on {BookFile}. Defaulting to no cover image", fileFilePath);
            }
            
            return Array.Empty<byte>();
        }
        
        private static string RemoveWhiteSpaceFromStylesheets(string body)
        {
            body = Regex.Replace(body, @"[a-zA-Z]+#", "#");
            body = Regex.Replace(body, @"[\n\r]+\s*", string.Empty);
            body = Regex.Replace(body, @"\s+", " ");
            body = Regex.Replace(body, @"\s?([:,;{}])\s?", "$1");
            body = body.Replace(";}", "}");
            body = Regex.Replace(body, @"([\s:]0)(px|pt|%|em)", "$1");

            // Remove comments from CSS
            body = Regex.Replace(body, @"/\*[\d\D]*?\*/", string.Empty);

            return body;
        }
    }
}