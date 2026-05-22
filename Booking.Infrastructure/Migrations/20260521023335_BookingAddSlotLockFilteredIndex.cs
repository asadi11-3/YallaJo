using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingAddSlotLockFilteredIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredNotificationSentAt",
                schema: "booking",
                table: "ProviderDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiringNotificationSentAt",
                schema: "booking",
                table: "ProviderDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_Status_UpdatedAt",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_SlotLock_User_Slot_Active",
                schema: "booking",
                table: "SlotLocks",
                columns: new[] { "UserId", "AvailabilitySlotId" },
                unique: true,
                filter: "[IsReleased] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_Status_ExpiresAt_ExpiredNotification",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "Status", "ExpiresAt", "ExpiredNotificationSentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_Status_ExpiresAt_ExpiringNotification",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "Status", "ExpiresAt", "ExpiringNotificationSentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourBookings_Status_UpdatedAt",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "UX_SlotLock_User_Slot_Active",
                schema: "booking",
                table: "SlotLocks");

            migrationBuilder.DropIndex(
                name: "IX_ProviderDocuments_Status_ExpiresAt_ExpiredNotification",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProviderDocuments_Status_ExpiresAt_ExpiringNotification",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropColumn(
                name: "ExpiredNotificationSentAt",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropColumn(
                name: "ExpiringNotificationSentAt",
                schema: "booking",
                table: "ProviderDocuments");
        }
    }
}
