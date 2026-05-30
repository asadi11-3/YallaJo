using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingWorkflowPhase5a : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GuideId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                schema: "booking",
                table: "TourBookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "JoinedFromBookingId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "JoinRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "booking",
                table: "JoinRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ResultingBookingId",
                schema: "booking",
                table: "JoinRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuideDiscounts",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: true),
                    CurrentUsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideDiscounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuideDiscounts_GuideUserId",
                schema: "booking",
                table: "GuideDiscounts",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuideDiscounts_GuideUserId_TourId",
                schema: "booking",
                table: "GuideDiscounts",
                columns: new[] { "GuideUserId", "TourId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideDiscounts_IsActive_ValidFrom_ValidUntil",
                schema: "booking",
                table: "GuideDiscounts",
                columns: new[] { "IsActive", "ValidFrom", "ValidUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuideDiscounts",
                schema: "booking");

            migrationBuilder.DropColumn(
                name: "GuideId",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "JoinedFromBookingId",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "booking",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "ResultingBookingId",
                schema: "booking",
                table: "JoinRequests");
        }
    }
}
