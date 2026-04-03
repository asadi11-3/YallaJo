using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentPlaces.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content_places");

            migrationBuilder.CreateTable(
                name: "AccessibilityFeatures",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessType = table.Column<int>(type: "int", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    Address = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetaTitle = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    LicenseNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    TaxId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubscriptionTier = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "content_places",
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
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaceBusinesses",
                schema: "content_places",
                columns: table => new
                {
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceBusinesses", x => new { x.PlaceId, x.BusinessId });
                });

            migrationBuilder.CreateTable(
                name: "Places",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlaceType = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    Address = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsWheelchairAccessible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasAudioGuide = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HasBrailleSignage = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MetaTitle = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessAmenities",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAmenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessAmenities_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessHours",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessHours_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessStaff",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessStaff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessStaff_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessTranslations",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessTranslations_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceItems",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    SalePriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    DiscountValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceItems_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "content_places",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceTranslations",
                schema: "content_places",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceTranslations_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalSchema: "content_places",
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityFeatures_EntityType_EntityId",
                schema: "content_places",
                table: "AccessibilityFeatures",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityFeatures_FeatureType",
                schema: "content_places",
                table: "AccessibilityFeatures",
                column: "FeatureType");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAmenities_BusinessId",
                schema: "content_places",
                table: "BusinessAmenities",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_OwnerId",
                schema: "content_places",
                table: "Businesses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_PlaceId",
                schema: "content_places",
                table: "Businesses",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Slug",
                schema: "content_places",
                table: "Businesses",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Status",
                schema: "content_places",
                table: "Businesses",
                column: "Status",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_BusinessId",
                schema: "content_places",
                table: "BusinessHours",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaff_BusinessId",
                schema: "content_places",
                table: "BusinessStaff",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaff_BusinessId_UserId",
                schema: "content_places",
                table: "BusinessStaff",
                columns: new[] { "BusinessId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaff_UserId",
                schema: "content_places",
                table: "BusinessStaff",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessTranslations_BusinessId_LanguageId",
                schema: "content_places",
                table: "BusinessTranslations",
                columns: new[] { "BusinessId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessTranslations_LanguageId",
                schema: "content_places",
                table: "BusinessTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAt",
                schema: "content_places",
                table: "InboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Unprocessed",
                schema: "content_places",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "RetryCount", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceBusinesses_BusinessId",
                schema: "content_places",
                table: "PlaceBusinesses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_IsFeatured",
                schema: "content_places",
                table: "Places",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Places_IsVerified",
                schema: "content_places",
                table: "Places",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Slug",
                schema: "content_places",
                table: "Places",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaceTranslations_LanguageId",
                schema: "content_places",
                table: "PlaceTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceTranslations_PlaceId_LanguageId",
                schema: "content_places",
                table: "PlaceTranslations",
                columns: new[] { "PlaceId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItems_BusinessId",
                schema: "content_places",
                table: "ServiceItems",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessibilityFeatures",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "BusinessAmenities",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "BusinessHours",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "BusinessStaff",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "BusinessTranslations",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "PlaceBusinesses",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "PlaceTranslations",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "ServiceItems",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "Places",
                schema: "content_places");

            migrationBuilder.DropTable(
                name: "Businesses",
                schema: "content_places");
        }
    }
}
