using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TourGuideFlowPhase6b_Dashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuideAvailabilityBlocks",
                schema: "content_tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideAvailabilityBlocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks",
                column: "GuideId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId_StartDate",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks",
                columns: new[] { "GuideId", "StartDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuideAvailabilityBlocks",
                schema: "content_tours");
        }
    }
}
