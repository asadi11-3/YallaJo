using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TourGuideFlowPhase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tours_PlaceId",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_TourGuides_IsActive",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlaceId",
                schema: "content_tours",
                table: "Tours",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExclusive",
                schema: "content_tours",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOpenForApplications",
                schema: "content_tours",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OwnershipType",
                schema: "content_tours",
                table: "Tours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProposedByGuideId",
                schema: "content_tours",
                table: "Tours",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                schema: "content_tours",
                table: "TourGuides",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                schema: "content_tours",
                table: "TourGuides",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedTourCount",
                schema: "content_tours",
                table: "TourGuides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                schema: "content_tours",
                table: "TourGuides",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                schema: "content_tours",
                table: "TourGuides",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedProviderId",
                schema: "content_tours",
                table: "TourGuides",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportCount",
                schema: "content_tours",
                table: "TourGuides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "content_tours",
                table: "TourGuides",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "content_tours",
                table: "TourGuides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                schema: "content_tours",
                table: "TourGuides",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuspendedByAdminId",
                schema: "content_tours",
                table: "TourGuides",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                schema: "content_tours",
                table: "TourGuides",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrustTier",
                schema: "content_tours",
                table: "TourGuides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GuideApplications",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProposedScheduleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedBasePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    RelevantExperience = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResubmissionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuidePricingTiers",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideTourOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PriceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MinParticipants = table.Column<int>(type: "int", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuidePricingTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuideSchedules",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideTourOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuideTourOfferings",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OffersPrivateTour = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PrivateTourPriceMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PrivateTourFlatPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    IsProposer = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuspendedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideTourOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuideTourOfferings_Tours_TourId",
                        column: x => x.TourId,
                        principalSchema: "content_tours",
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourProposals",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourGuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ShortDescription = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PlaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxGroupSize = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RequestExclusive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedTourId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourProposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_PlaceId",
                schema: "content_tours",
                table: "Tours",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_Slug",
                schema: "content_tours",
                table: "TourGuides",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_Status",
                schema: "content_tours",
                table: "TourGuides",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GuideApplications_GuideUserId",
                schema: "content_tours",
                table: "GuideApplications",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideApplications_TourId_TourGuideId",
                schema: "content_tours",
                table: "GuideApplications",
                columns: new[] { "TourId", "TourGuideId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuidePricingTiers_GuideTourOfferingId",
                schema: "content_tours",
                table: "GuidePricingTiers",
                column: "GuideTourOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuidePricingTiers_TourGuideId_TourId",
                schema: "content_tours",
                table: "GuidePricingTiers",
                columns: new[] { "TourGuideId", "TourId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideSchedules_GuideTourOfferingId",
                schema: "content_tours",
                table: "GuideSchedules",
                column: "GuideTourOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideSchedules_TourGuideId_TourId",
                schema: "content_tours",
                table: "GuideSchedules",
                columns: new[] { "TourGuideId", "TourId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideTourOfferings_TourGuideId",
                schema: "content_tours",
                table: "GuideTourOfferings",
                column: "TourGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideTourOfferings_TourId_TourGuideId",
                schema: "content_tours",
                table: "GuideTourOfferings",
                columns: new[] { "TourId", "TourGuideId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TourProposals_GuideUserId",
                schema: "content_tours",
                table: "TourProposals",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TourProposals_TourGuideId",
                schema: "content_tours",
                table: "TourProposals",
                column: "TourGuideId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuideApplications",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "GuidePricingTiers",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "GuideSchedules",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "GuideTourOfferings",
                schema: "content_tours");

            migrationBuilder.DropTable(
                name: "TourProposals",
                schema: "content_tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_PlaceId",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_TourGuides_Slug",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropIndex(
                name: "IX_TourGuides_Status",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "IsExclusive",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsOpenForApplications",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "ProposedByGuideId",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "CompletedTourCount",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "LinkedProviderId",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "ReportCount",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "SuspendedByAdminId",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.DropColumn(
                name: "TrustTier",
                schema: "content_tours",
                table: "TourGuides");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlaceId",
                schema: "content_tours",
                table: "Tours",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "content_tours",
                table: "TourGuides",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_PlaceId",
                schema: "content_tours",
                table: "Tours",
                column: "PlaceId",
                filter: "[PlaceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuides_IsActive",
                schema: "content_tours",
                table: "TourGuides",
                column: "IsActive");
        }
    }
}
