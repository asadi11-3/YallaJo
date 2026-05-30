using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentBlogs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeCreatorPostIntoBlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsFeatured",
                schema: "content_blogs",
                table: "Blogs",
                newName: "IsSponsored");

            migrationBuilder.AddColumn<Guid>(
                name: "AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommentCount",
                schema: "content_blogs",
                table: "Blogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DisclosedTargets",
                schema: "content_blogs",
                table: "Blogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeaturedAt",
                schema: "content_blogs",
                table: "Blogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FeaturedByAdminId",
                schema: "content_blogs",
                table: "Blogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeaturedUntil",
                schema: "content_blogs",
                table: "Blogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                schema: "content_blogs",
                table: "Blogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ReactionCount",
                schema: "content_blogs",
                table: "Blogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "content_blogs",
                table: "Blogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportCount",
                schema: "content_blogs",
                table: "Blogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                schema: "content_blogs",
                table: "Blogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByAdminId",
                schema: "content_blogs",
                table: "Blogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                schema: "content_blogs",
                table: "Blogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CreatorApplications",
                schema: "content_blogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PortfolioUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleWorkUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NicheIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FreeTags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanguageIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredRegionIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialHandles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReapplicationCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastRejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreatorInvitations",
                schema: "content_blogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    InvitedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonalMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RedeemedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorInvitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreatorNiches",
                schema: "content_blogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorNiches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreatorProfiles",
                schema: "content_blogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrustTier = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SuspensionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuspendedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArticleCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalReactionCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalCommentCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    FollowerCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReportCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReportRate = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    EligibleForTier1 = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    EligibleForTier2 = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LinkedProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreatorFollows",
                schema: "content_blogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FollowerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreatorFollows_CreatorProfiles_CreatorProfileId",
                        column: x => x.CreatorProfileId,
                        principalSchema: "content_blogs",
                        principalTable: "CreatorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs",
                column: "AuthoredByCreatorId",
                filter: "[AuthoredByCreatorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorApplications_ApplicantUserId",
                schema: "content_blogs",
                table: "CreatorApplications",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorApplications_Status",
                schema: "content_blogs",
                table: "CreatorApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorFollows_CreatorProfileId",
                schema: "content_blogs",
                table: "CreatorFollows",
                column: "CreatorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorFollows_FollowerUserId_CreatorProfileId",
                schema: "content_blogs",
                table: "CreatorFollows",
                columns: new[] { "FollowerUserId", "CreatorProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorInvitations_Email",
                schema: "content_blogs",
                table: "CreatorInvitations",
                column: "Email",
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorInvitations_InvitedUserId",
                schema: "content_blogs",
                table: "CreatorInvitations",
                column: "InvitedUserId",
                filter: "[InvitedUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorInvitations_Status_ExpiresAt",
                schema: "content_blogs",
                table: "CreatorInvitations",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreatorInvitations_Token",
                schema: "content_blogs",
                table: "CreatorInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorNiches_Slug",
                schema: "content_blogs",
                table: "CreatorNiches",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_ApplicationId",
                schema: "content_blogs",
                table: "CreatorProfiles",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_Slug",
                schema: "content_blogs",
                table: "CreatorProfiles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_UserId",
                schema: "content_blogs",
                table: "CreatorProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreatorApplications",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorFollows",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorInvitations",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorNiches",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorProfiles",
                schema: "content_blogs");

            migrationBuilder.DropIndex(
                name: "IX_Blogs_AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "CommentCount",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "DisclosedTargets",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "FeaturedAt",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "FeaturedByAdminId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "FeaturedUntil",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ReactionCount",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ReportCount",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.RenameColumn(
                name: "IsSponsored",
                schema: "content_blogs",
                table: "Blogs",
                newName: "IsFeatured");
        }
    }
}
