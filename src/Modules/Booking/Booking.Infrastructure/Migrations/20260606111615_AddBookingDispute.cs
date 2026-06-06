using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDispute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DisputeOpenedByUserId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputedAt",
                schema: "booking",
                table: "TourBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                schema: "booking",
                table: "TourBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedByAdminId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisputeOpenedByUserId",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "DisputedAt",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ResolvedByAdminId",
                schema: "booking",
                table: "TourBookings");
        }
    }
}
