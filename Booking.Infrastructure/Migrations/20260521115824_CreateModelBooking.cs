using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModelBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SlotLocks_ExpiresAt_Active",
                schema: "booking",
                table: "SlotLocks",
                column: "ExpiresAt",
                filter: "[IsReleased] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_TourId_Date_Active",
                schema: "booking",
                table: "AvailabilitySlots",
                columns: new[] { "TourId", "Date" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SlotLocks_ExpiresAt_Active",
                schema: "booking",
                table: "SlotLocks");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilitySlots_TourId_Date_Active",
                schema: "booking",
                table: "AvailabilitySlots");
        }
    }
}
