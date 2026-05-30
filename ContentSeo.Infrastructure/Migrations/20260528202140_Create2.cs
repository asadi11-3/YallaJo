using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentSeo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Create2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content_seo");

            migrationBuilder.CreateTable(
                name: "FaqItems",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
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
                    table.PrimaryKey("PK_FaqItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TraceContext = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Redirects",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    NewUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HitCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Redirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeoMetadata",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    OgTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OgDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OgImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SchemaMarkup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SitemapPriority = table.Column<decimal>(type: "decimal(2,1)", precision: 2, scale: 1, nullable: false, defaultValue: 0.5m),
                    SitemapChangeFrequency = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SitemapEntries",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    ChangeFrequency = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Priority = table.Column<decimal>(type: "decimal(2,1)", precision: 2, scale: 1, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SitemapEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherCache",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoundedLatitude = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    RoundedLongitude = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    FeelsLike = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Humidity = table.Column<int>(type: "int", nullable: true),
                    WindSpeed = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    WindDirection = table.Column<int>(type: "int", nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UvIndex = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    ForecastJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherDailyBudget",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CallsUsed = table.Column<int>(type: "int", nullable: false),
                    DailyLimit = table.Column<int>(type: "int", nullable: false),
                    AlertSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherDailyBudget", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaqItemTranslations",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FaqItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqItemTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaqItemTranslations_FaqItems_FaqItemId",
                        column: x => x.FaqItemId,
                        principalSchema: "content_seo",
                        principalTable: "FaqItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_EntityType_EntityId",
                schema: "content_seo",
                table: "FaqItems",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_FaqItemTranslations_FaqItemId",
                schema: "content_seo",
                table: "FaqItemTranslations",
                column: "FaqItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAt",
                schema: "content_seo",
                table: "InboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Unprocessed",
                schema: "content_seo",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "RetryCount", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Redirects_OldUrl",
                schema: "content_seo",
                table: "Redirects",
                column: "OldUrl",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SeoMetadata_EntityType_EntityId",
                schema: "content_seo",
                table: "SeoMetadata",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SitemapEntries_EntityType_EntityId",
                schema: "content_seo",
                table: "SitemapEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SitemapEntries_Url",
                schema: "content_seo",
                table: "SitemapEntries",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherCache_ExpiresAt",
                schema: "content_seo",
                table: "WeatherCache",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherCache_PlaceId",
                schema: "content_seo",
                table: "WeatherCache",
                column: "PlaceId",
                filter: "[PlaceId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_WeatherCache_Lat_Lng_Date",
                schema: "content_seo",
                table: "WeatherCache",
                columns: new[] { "RoundedLatitude", "RoundedLongitude", "ForecastDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_WeatherDailyBudget_Date",
                schema: "content_seo",
                table: "WeatherDailyBudget",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqItemTranslations",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "Redirects",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "SeoMetadata",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "SitemapEntries",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "WeatherCache",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "WeatherDailyBudget",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "FaqItems",
                schema: "content_seo");
        }
    }
}
