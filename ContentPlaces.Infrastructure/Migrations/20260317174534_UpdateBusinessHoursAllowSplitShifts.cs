using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentPlaces.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBusinessHoursAllowSplitShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessHours_BusinessId_DayOfWeek",
                schema: "content_places",
                table: "BusinessHours");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_BusinessId",
                schema: "content_places",
                table: "BusinessHours",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessHours_BusinessId",
                schema: "content_places",
                table: "BusinessHours");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_BusinessId_DayOfWeek",
                schema: "content_places",
                table: "BusinessHours",
                columns: new[] { "BusinessId", "DayOfWeek" },
                unique: true);
        }
    }
}
