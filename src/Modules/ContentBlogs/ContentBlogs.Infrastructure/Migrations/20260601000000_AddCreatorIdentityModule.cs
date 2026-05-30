using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentBlogs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorIdentityModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. CreatorNiches ──────────────────────────────────────────────
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

            migrationBuilder.CreateIndex(
                name: "IX_CreatorNiches_Slug",
                schema: "content_blogs",
                table: "CreatorNiches",
                column: "Slug",
                unique: true);

            // ── 2. CreatorApplications ────────────────────────────────────────
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
                    PortfolioUrls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SampleWorkUrls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NicheIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FreeTags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LanguageIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreferredRegionIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SocialHandles = table.Column<string>(type: "nvarchar(max)", nullable: false),
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

            // ── 3. CreatorProfiles ────────────────────────────────────────────
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
                    TrustTier = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ArticleCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalReactionCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalCommentCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    FollowerCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_UserId",
                schema: "content_blogs",
                table: "CreatorProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_Slug",
                schema: "content_blogs",
                table: "CreatorProfiles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_ApplicationId",
                schema: "content_blogs",
                table: "CreatorProfiles",
                column: "ApplicationId");

            // ── 4. CreatorInvitations ─────────────────────────────────────────
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
                    SentByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonalMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_CreatorInvitations_Token",
                schema: "content_blogs",
                table: "CreatorInvitations",
                column: "Token",
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

            // ── 5. CreatorFollows ─────────────────────────────────────────────
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreatorFollows_FollowerUserId_CreatorProfileId",
                schema: "content_blogs",
                table: "CreatorFollows",
                columns: new[] { "FollowerUserId", "CreatorProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorFollows_CreatorProfileId",
                schema: "content_blogs",
                table: "CreatorFollows",
                column: "CreatorProfileId");

            // ── 6. Blogs table — add AuthoredByCreatorId + LanguageId ────────
            migrationBuilder.AddColumn<Guid>(
                name: "AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                schema: "content_blogs",
                table: "Blogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs",
                column: "AuthoredByCreatorId",
                filter: "[AuthoredByCreatorId] IS NOT NULL");

            // ── 7. Seed CreatorNiches ─────────────────────────────────────────
            SeedCreatorNiches(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Blogs_AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "AuthoredByCreatorId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropTable(
                name: "CreatorFollows",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorInvitations",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorProfiles",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorApplications",
                schema: "content_blogs");

            migrationBuilder.DropTable(
                name: "CreatorNiches",
                schema: "content_blogs");
        }

        private static void SeedCreatorNiches(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var niches = new (string Name, string Slug, string Description, int SortOrder)[]
            {
                ("Historical & Cultural", "historical-cultural", "Content focused on historical sites, cultural heritage, and traditions", 1),
                ("Adventure & Outdoor", "adventure-outdoor", "Outdoor activities, hiking, extreme sports, and adventure travel", 2),
                ("Food & Culinary", "food-culinary", "Local cuisine, restaurants, street food, and culinary experiences", 3),
                ("Family & Kids", "family-kids", "Family-friendly destinations, activities for children, and family travel tips", 4),
                ("Luxury & Wellness", "luxury-wellness", "Premium travel experiences, spas, resorts, and wellness retreats", 5),
                ("Budget & Backpacking", "budget-backpacking", "Affordable travel tips, hostels, and budget-friendly destinations", 6),
                ("Photography & Visual", "photography-visual", "Photography tours, scenic spots, and visual storytelling", 7),
                ("Eco & Sustainable", "eco-sustainable", "Eco-tourism, sustainable travel practices, and nature conservation", 8),
                ("Nightlife & Entertainment", "nightlife-entertainment", "Nightlife, events, festivals, and entertainment guides", 9),
                ("Religious & Spiritual", "religious-spiritual", "Religious sites, pilgrimage routes, and spiritual journeys", 10)
            };

            // Deterministic GUIDs for reproducible seeding
            var ids = new Guid[]
            {
                new("a0000001-0000-0000-0000-000000000001"),
                new("a0000001-0000-0000-0000-000000000002"),
                new("a0000001-0000-0000-0000-000000000003"),
                new("a0000001-0000-0000-0000-000000000004"),
                new("a0000001-0000-0000-0000-000000000005"),
                new("a0000001-0000-0000-0000-000000000006"),
                new("a0000001-0000-0000-0000-000000000007"),
                new("a0000001-0000-0000-0000-000000000008"),
                new("a0000001-0000-0000-0000-000000000009"),
                new("a0000001-0000-0000-0000-00000000000a")
            };

            for (var i = 0; i < niches.Length; i++)
            {
                var (name, slug, description, sortOrder) = niches[i];
                migrationBuilder.InsertData(
                    schema: "content_blogs",
                    table: "CreatorNiches",
                    columns: new[] { "Id", "Name", "Slug", "Description", "SortOrder", "IsActive", "CreatedAt" },
                    values: new object[] { ids[i], name, slug, description, sortOrder, true, now });
            }
        }
    }
}
