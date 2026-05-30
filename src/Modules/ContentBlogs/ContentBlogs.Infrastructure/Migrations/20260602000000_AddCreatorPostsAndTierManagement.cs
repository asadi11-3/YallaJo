using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentBlogs.Infrastructure.Migrations;

/// <summary>
/// Wave-8: Adds CreatorPosts table, extends CreatorProfiles with tier management columns,
/// and adds disclosure columns to Blogs.
/// </summary>
public partial class AddCreatorPostsAndTierManagement : Migration
{
    private const string Schema = "content_blogs";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ──────────────────────────────────────────────────────────────────────
        // 1. CreatorPosts table
        // ──────────────────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "CreatorPosts",
            schema: Schema,
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PostType = table.Column<int>(type: "int", nullable: false),
                Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Excerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                FeaturedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                FeaturedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                FeaturedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsSponsored = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DisclosedTargets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                ReactionCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                CommentCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                ReportCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                TaggedEntityIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                TaggedEntityTypes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NicheIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                FreeTags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PlaceRegionIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                TypeSpecificDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CreatorPosts", x => x.Id);
            });

        // Indexes on CreatorPosts
        migrationBuilder.CreateIndex(
            name: "IX_CreatorPosts_Slug",
            schema: Schema,
            table: "CreatorPosts",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CreatorPosts_CreatorProfileId",
            schema: Schema,
            table: "CreatorPosts",
            column: "CreatorProfileId");

        migrationBuilder.CreateIndex(
            name: "IX_CreatorPosts_Status",
            schema: Schema,
            table: "CreatorPosts",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_CreatorPosts_IsFeatured_FeaturedUntil",
            schema: Schema,
            table: "CreatorPosts",
            columns: new[] { "IsFeatured", "FeaturedUntil" },
            filter: "[IsFeatured] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_CreatorPosts_Status_SubmittedAt",
            schema: Schema,
            table: "CreatorPosts",
            columns: new[] { "Status", "SubmittedAt" },
            filter: "[Status] = 1"); // PendingReview = 1

        // ──────────────────────────────────────────────────────────────────────
        // 2. Extend CreatorProfiles with tier management columns
        // ──────────────────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<int>(
            name: "PublishedPostCount",
            schema: Schema,
            table: "CreatorProfiles",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ReportCount",
            schema: Schema,
            table: "CreatorProfiles",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<double>(
            name: "ReportRate",
            schema: Schema,
            table: "CreatorProfiles",
            type: "float",
            nullable: false,
            defaultValue: 0.0);

        migrationBuilder.AddColumn<bool>(
            name: "EligibleForTier1",
            schema: Schema,
            table: "CreatorProfiles",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EligibleForTier2",
            schema: Schema,
            table: "CreatorProfiles",
            type: "bit",
            nullable: false,
            defaultValue: false);

        // ──────────────────────────────────────────────────────────────────────
        // 3. Extend Blogs with disclosure columns (retro-fit)
        // ──────────────────────────────────────────────────────────────────────
        migrationBuilder.AddColumn<bool>(
            name: "IsSponsored",
            schema: Schema,
            table: "Blogs",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "DisclosedTargets",
            schema: Schema,
            table: "Blogs",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ── Undo Blog disclosure columns ────────────────────────────────────
        migrationBuilder.DropColumn(name: "DisclosedTargets", schema: Schema, table: "Blogs");
        migrationBuilder.DropColumn(name: "IsSponsored", schema: Schema, table: "Blogs");

        // ── Undo CreatorProfile tier management columns ─────────────────────
        migrationBuilder.DropColumn(name: "EligibleForTier2", schema: Schema, table: "CreatorProfiles");
        migrationBuilder.DropColumn(name: "EligibleForTier1", schema: Schema, table: "CreatorProfiles");
        migrationBuilder.DropColumn(name: "ReportRate", schema: Schema, table: "CreatorProfiles");
        migrationBuilder.DropColumn(name: "ReportCount", schema: Schema, table: "CreatorProfiles");
        migrationBuilder.DropColumn(name: "PublishedPostCount", schema: Schema, table: "CreatorProfiles");

        // ── Drop CreatorPosts table ─────────────────────────────────────────
        migrationBuilder.DropTable(name: "CreatorPosts", schema: Schema);
    }
}
