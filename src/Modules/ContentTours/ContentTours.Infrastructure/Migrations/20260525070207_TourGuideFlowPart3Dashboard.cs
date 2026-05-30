using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TourGuideFlowPart3Dashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId_StartDate",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks");

            migrationBuilder.CreateIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId_StartDate_EndDate",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks",
                columns: new[] { "GuideId", "StartDate", "EndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_GuideAvailabilityBlocks_TourGuides_GuideId",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks",
                column: "GuideId",
                principalSchema: "content_tours",
                principalTable: "TourGuides",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuideAvailabilityBlocks_TourGuides_GuideId",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks");

            migrationBuilder.DropIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId_StartDate_EndDate",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks");

            migrationBuilder.CreateIndex(
                name: "IX_GuideAvailabilityBlocks_GuideId_StartDate",
                schema: "content_tours",
                table: "GuideAvailabilityBlocks",
                columns: new[] { "GuideId", "StartDate" });
        }
    }
}
