using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationCode",
                schema: "booking",
                table: "TourBookings",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsDiscount",
                schema: "booking",
                table: "TourBookings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PointsDiscountCurrency",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PointsRedeemed",
                schema: "booking",
                table: "TourBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralDiscount",
                schema: "booking",
                table: "TourBookings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferralDiscountCurrency",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "booking",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "booking",
                table: "Reservations",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceItemId",
                schema: "booking",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                schema: "booking",
                table: "Reservations",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TotalPriceCurrency",
                schema: "booking",
                table: "Reservations",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "TourGuideId",
                schema: "booking",
                table: "ProviderDocuments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                schema: "booking",
                table: "ProviderDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedCount",
                schema: "booking",
                table: "AvailabilitySlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceOverride",
                schema: "booking",
                table: "AvailabilitySlots",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceOverrideCurrency",
                schema: "booking",
                table: "AvailabilitySlots",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                schema: "booking",
                table: "AvailabilitySlots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceItemId",
                schema: "booking",
                table: "AvailabilitySlots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_ConfirmationCode",
                schema: "booking",
                table: "TourBookings",
                column: "ConfirmationCode",
                unique: true,
                filter: "[ConfirmationCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_BusinessId",
                schema: "booking",
                table: "ProviderDocuments",
                column: "BusinessId",
                filter: "[BusinessId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProviderDocuments_SingleTarget",
                schema: "booking",
                table: "ProviderDocuments",
                sql: "(CASE WHEN [TourGuideId] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [BusinessId] IS NOT NULL THEN 1 ELSE 0 END) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AvailSlots_Capacity",
                schema: "booking",
                table: "AvailabilitySlots",
                sql: "[BookedCount] + [LockedCount] <= [MaxCapacity]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourBookings_ConfirmationCode",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "IX_ProviderDocuments_BusinessId",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProviderDocuments_SingleTarget",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AvailSlots_Capacity",
                schema: "booking",
                table: "AvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ConfirmationCode",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "PointsDiscount",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "PointsDiscountCurrency",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "PointsRedeemed",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ReferralDiscount",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ReferralDiscountCurrency",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "booking",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "booking",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ServiceItemId",
                schema: "booking",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                schema: "booking",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "TotalPriceCurrency",
                schema: "booking",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropColumn(
                name: "LockedCount",
                schema: "booking",
                table: "AvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "PriceOverride",
                schema: "booking",
                table: "AvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "PriceOverrideCurrency",
                schema: "booking",
                table: "AvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                schema: "booking",
                table: "AvailabilitySlots");

            migrationBuilder.DropColumn(
                name: "ServiceItemId",
                schema: "booking",
                table: "AvailabilitySlots");

            migrationBuilder.AlterColumn<Guid>(
                name: "TourGuideId",
                schema: "booking",
                table: "ProviderDocuments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
