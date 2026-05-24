using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJoinRequestPendingUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JoinRequests_TourBookingId_UserId",
                schema: "booking",
                table: "JoinRequests");

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_TourBookingId",
                schema: "booking",
                table: "JoinRequests",
                column: "TourBookingId");

            migrationBuilder.CreateIndex(
                name: "UX_JoinRequests_Pending_TourBookingId_UserId",
                schema: "booking",
                table: "JoinRequests",
                columns: new[] { "TourBookingId", "UserId" },
                unique: true,
                filter: "[Status] = 0 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JoinRequests_TourBookingId",
                schema: "booking",
                table: "JoinRequests");

            migrationBuilder.DropIndex(
                name: "UX_JoinRequests_Pending_TourBookingId_UserId",
                schema: "booking",
                table: "JoinRequests");

            migrationBuilder.CreateIndex(
                name: "IX_JoinRequests_TourBookingId_UserId",
                schema: "booking",
                table: "JoinRequests",
                columns: new[] { "TourBookingId", "UserId" });
        }
    }
}
