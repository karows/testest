﻿// <auto-generated />
using System;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20210901020400_ReadingLists")]
    partial class ReadingLists
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.8");

            modelBuilder.Entity("API.Entities.AppRole", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("API.Entities.AppUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ApiKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastActive")
                        .HasColumnType("TEXT");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("RowVersion")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("TEXT");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("API.Entities.AppUserBookmark", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AppUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ChapterId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Page")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeriesId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("VolumeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AppUserId");

                    b.ToTable("AppUserBookmark");
                });

            modelBuilder.Entity("API.Entities.AppUserPreferences", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AppUserId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AutoCloseMenu")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("BookReaderDarkMode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("BookReaderFontFamily")
                        .HasColumnType("TEXT");

                    b.Property<int>("BookReaderFontSize")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BookReaderLineSpacing")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BookReaderMargin")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BookReaderReadingDirection")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("BookReaderTapToPaginate")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PageSplitOption")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReaderMode")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReadingDirection")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ScalingOption")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("SiteDarkMode")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AppUserId")
                        .IsUnique();

                    b.ToTable("AppUserPreferences");
                });

            modelBuilder.Entity("API.Entities.AppUserProgress", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AppUserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("BookScrollId")
                        .HasColumnType("TEXT");

                    b.Property<int>("ChapterId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<int>("PagesRead")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("RowVersion")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeriesId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("VolumeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AppUserId");

                    b.ToTable("AppUserProgresses");
                });

            modelBuilder.Entity("API.Entities.AppUserRating", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AppUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Rating")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Review")
                        .HasColumnType("TEXT");

                    b.Property<int>("SeriesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AppUserId");

                    b.ToTable("AppUserRating");
                });

            modelBuilder.Entity("API.Entities.AppUserRole", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RoleId")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("API.Entities.Chapter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("CoverImage")
                        .HasColumnType("BLOB");

                    b.Property<bool>("CoverImageLocked")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSpecial")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Number")
                        .HasColumnType("TEXT");

                    b.Property<int>("Pages")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Range")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<int>("VolumeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("VolumeId");

                    b.ToTable("Chapter");
                });

            modelBuilder.Entity("API.Entities.CollectionTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("CoverImage")
                        .HasColumnType("BLOB");

                    b.Property<bool>("CoverImageLocked")
                        .HasColumnType("INTEGER");

                    b.Property<string>("NormalizedTitle")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Promoted")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("RowVersion")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Summary")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id", "Promoted")
                        .IsUnique();

                    b.ToTable("CollectionTag");
                });

            modelBuilder.Entity("API.Entities.FolderPath", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastScanned")
                        .HasColumnType("TEXT");

                    b.Property<int>("LibraryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("LibraryId");

                    b.ToTable("FolderPath");
                });

            modelBuilder.Entity("API.Entities.Library", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CoverImage")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Library");
                });

            modelBuilder.Entity("API.Entities.MangaFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ChapterId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FilePath")
                        .HasColumnType("TEXT");

                    b.Property<int>("Format")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<int>("Pages")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ChapterId");

                    b.ToTable("MangaFile");
                });

            modelBuilder.Entity("API.Entities.ReadingList", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AppUserId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Promoted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Summary")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AppUserId");

                    b.ToTable("ReadingList");
                });

            modelBuilder.Entity("API.Entities.ReadingListItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ChapterId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LibraryId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ReadingListId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeriesId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("VolumeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ReadingListId");

                    b.ToTable("ReadingListItem");
                });

            modelBuilder.Entity("API.Entities.Series", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("CoverImage")
                        .HasColumnType("BLOB");

                    b.Property<bool>("CoverImageLocked")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<int>("Format")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<int>("LibraryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LocalizedName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedName")
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalName")
                        .HasColumnType("TEXT");

                    b.Property<int>("Pages")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SortName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Summary")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("LibraryId");

                    b.HasIndex("Name", "NormalizedName", "LocalizedName", "LibraryId", "Format")
                        .IsUnique();

                    b.ToTable("Series");
                });

            modelBuilder.Entity("API.Entities.SeriesMetadata", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<uint>("RowVersion")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeriesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SeriesId")
                        .IsUnique();

                    b.HasIndex("Id", "SeriesId")
                        .IsUnique();

                    b.ToTable("SeriesMetadata");
                });

            modelBuilder.Entity("API.Entities.ServerSetting", b =>
                {
                    b.Property<int>("Key")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("RowVersion")
                        .IsConcurrencyToken()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Key");

                    b.ToTable("ServerSetting");
                });

            modelBuilder.Entity("API.Entities.Volume", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("CoverImage")
                        .HasColumnType("BLOB");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastModified")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Pages")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeriesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SeriesId");

                    b.ToTable("Volume");
                });

            modelBuilder.Entity("AppUserLibrary", b =>
                {
                    b.Property<int>("AppUsersId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LibrariesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("AppUsersId", "LibrariesId");

                    b.HasIndex("LibrariesId");

                    b.ToTable("AppUserLibrary");
                });

            modelBuilder.Entity("CollectionTagSeriesMetadata", b =>
                {
                    b.Property<int>("CollectionTagsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SeriesMetadatasId")
                        .HasColumnType("INTEGER");

                    b.HasKey("CollectionTagsId", "SeriesMetadatasId");

                    b.HasIndex("SeriesMetadatasId");

                    b.ToTable("CollectionTagSeriesMetadata");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<int>("RoleId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("TEXT");

                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("API.Entities.AppUserBookmark", b =>
                {
                    b.HasOne("API.Entities.AppUser", "AppUser")
                        .WithMany("Bookmarks")
                        .HasForeignKey("AppUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("API.Entities.AppUserPreferences", b =>
                {
                    b.HasOne("API.Entities.AppUser", "AppUser")
                        .WithOne("UserPreferences")
                        .HasForeignKey("API.Entities.AppUserPreferences", "AppUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("API.Entities.AppUserProgress", b =>
                {
                    b.HasOne("API.Entities.AppUser", "AppUser")
                        .WithMany("Progresses")
                        .HasForeignKey("AppUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("API.Entities.AppUserRating", b =>
                {
                    b.HasOne("API.Entities.AppUser", "AppUser")
                        .WithMany("Ratings")
                        .HasForeignKey("AppUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("API.Entities.AppUserRole", b =>
                {
                    b.HasOne("API.Entities.AppRole", "Role")
                        .WithMany("UserRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Entities.AppUser", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("API.Entities.Chapter", b =>
                {
                    b.HasOne("API.Entities.Volume", "Volume")
                        .WithMany("Chapters")
                        .HasForeignKey("VolumeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Volume");
                });

            modelBuilder.Entity("API.Entities.FolderPath", b =>
                {
                    b.HasOne("API.Entities.Library", "Library")
                        .WithMany("Folders")
                        .HasForeignKey("LibraryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Library");
                });

            modelBuilder.Entity("API.Entities.MangaFile", b =>
                {
                    b.HasOne("API.Entities.Chapter", "Chapter")
                        .WithMany("Files")
                        .HasForeignKey("ChapterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chapter");
                });

            modelBuilder.Entity("API.Entities.ReadingList", b =>
                {
                    b.HasOne("API.Entities.AppUser", "AppUser")
                        .WithMany("ReadingLists")
                        .HasForeignKey("AppUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AppUser");
                });

            modelBuilder.Entity("API.Entities.ReadingListItem", b =>
                {
                    b.HasOne("API.Entities.ReadingList", null)
                        .WithMany("Items")
                        .HasForeignKey("ReadingListId");
                });

            modelBuilder.Entity("API.Entities.Series", b =>
                {
                    b.HasOne("API.Entities.Library", "Library")
                        .WithMany("Series")
                        .HasForeignKey("LibraryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Library");
                });

            modelBuilder.Entity("API.Entities.SeriesMetadata", b =>
                {
                    b.HasOne("API.Entities.Series", "Series")
                        .WithOne("Metadata")
                        .HasForeignKey("API.Entities.SeriesMetadata", "SeriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Series");
                });

            modelBuilder.Entity("API.Entities.Volume", b =>
                {
                    b.HasOne("API.Entities.Series", "Series")
                        .WithMany("Volumes")
                        .HasForeignKey("SeriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Series");
                });

            modelBuilder.Entity("AppUserLibrary", b =>
                {
                    b.HasOne("API.Entities.AppUser", null)
                        .WithMany()
                        .HasForeignKey("AppUsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Entities.Library", null)
                        .WithMany()
                        .HasForeignKey("LibrariesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("CollectionTagSeriesMetadata", b =>
                {
                    b.HasOne("API.Entities.CollectionTag", null)
                        .WithMany()
                        .HasForeignKey("CollectionTagsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("API.Entities.SeriesMetadata", null)
                        .WithMany()
                        .HasForeignKey("SeriesMetadatasId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("API.Entities.AppRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("API.Entities.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("API.Entities.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("API.Entities.AppUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("API.Entities.AppRole", b =>
                {
                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("API.Entities.AppUser", b =>
                {
                    b.Navigation("Bookmarks");

                    b.Navigation("Progresses");

                    b.Navigation("Ratings");

                    b.Navigation("ReadingLists");

                    b.Navigation("UserPreferences");

                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("API.Entities.Chapter", b =>
                {
                    b.Navigation("Files");
                });

            modelBuilder.Entity("API.Entities.Library", b =>
                {
                    b.Navigation("Folders");

                    b.Navigation("Series");
                });

            modelBuilder.Entity("API.Entities.ReadingList", b =>
                {
                    b.Navigation("Items");
                });

            modelBuilder.Entity("API.Entities.Series", b =>
                {
                    b.Navigation("Metadata");

                    b.Navigation("Volumes");
                });

            modelBuilder.Entity("API.Entities.Volume", b =>
                {
                    b.Navigation("Chapters");
                });
#pragma warning restore 612, 618
        }
    }
}
