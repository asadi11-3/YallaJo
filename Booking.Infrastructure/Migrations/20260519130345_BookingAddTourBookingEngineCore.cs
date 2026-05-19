using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingAddTourBookingEngineCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourBookings_ConfirmationCode",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "IX_TourBookings_UserId_ScheduledDate",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ConfirmationCode",
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
                name: "ReferralDiscountCurrency",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "TotalPriceCurrency",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.RenameColumn(
                name: "TourGuideId",
                schema: "booking",
                table: "TourBookings",
                newName: "CompletedByUserId");

            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                schema: "booking",
                table: "TourBookings",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "ReferralDiscount",
                schema: "booking",
                table: "TourBookings",
                newName: "Subtotal");

            migrationBuilder.RenameColumn(
                name: "PointsDiscount",
                schema: "booking",
                table: "TourBookings",
                newName: "CommissionAmount");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "booking",
                table: "TourBookings",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationSource",
                schema: "booking",
                table: "TourBookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                schema: "booking",
                table: "TourBookings",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ConfirmationSource",
                schema: "booking",
                table: "TourBookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "booking",
                table: "TourBookings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsInstantBooking",
                schema: "booking",
                table: "TourBookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LineItemsJson",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyAmount",
                schema: "booking",
                table: "TourBookings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentExpiresAt",
                schema: "booking",
                table: "TourBookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                schema: "booking",
                table: "TourBookings",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                schema: "booking",
                table: "TourBookings",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundPolicySnapshot",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                schema: "booking",
                table: "TourBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                schema: "booking",
                table: "SlotLocks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParticipantCount",
                schema: "booking",
                table: "SlotLocks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_AvailabilitySlotId",
                schema: "booking",
                table: "TourBookings",
                column: "AvailabilitySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_ProviderId_Status",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "ProviderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_Reference",
                schema: "booking",
                table: "TourBookings",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_TourId_Status",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "TourId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_UserId_Status",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotLocks_BookingId",
                schema: "booking",
                table: "SlotLocks",
                column: "BookingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourBookings_AvailabilitySlotId",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "IX_TourBookings_ProviderId_Status",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "IX_TourBookings_Reference",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "IX_TourBookings_TourId_Status",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "IX_TourBookings_UserId_Status",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropIndex(
                name: "IX_SlotLocks_BookingId",
                schema: "booking",
                table: "SlotLocks");

            migrationBuilder.DropColumn(
                name: "CancellationSource",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "CommissionRate",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ConfirmationSource",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "IsInstantBooking",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "LineItemsJson",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "LoyaltyAmount",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "PaymentExpiresAt",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "Reference",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "RefundPolicySnapshot",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "booking",
                table: "TourBookings");

            migrationBuilder.DropColumn(
                name: "BookingId",
                schema: "booking",
                table: "SlotLocks");

            migrationBuilder.DropColumn(
                name: "ParticipantCount",
                schema: "booking",
                table: "SlotLocks");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                schema: "booking",
                table: "TourBookings",
                newName: "TotalPrice");

            migrationBuilder.RenameColumn(
                name: "Subtotal",
                schema: "booking",
                table: "TourBookings",
                newName: "ReferralDiscount");

            migrationBuilder.RenameColumn(
                name: "CompletedByUserId",
                schema: "booking",
                table: "TourBookings",
                newName: "TourGuideId");

            migrationBuilder.RenameColumn(
                name: "CommissionAmount",
                schema: "booking",
                table: "TourBookings",
                newName: "PointsDiscount");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldUnicode: false,
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AvailabilitySlotId",
                schema: "booking",
                table: "TourBookings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationCode",
                schema: "booking",
                table: "TourBookings",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "ReferralDiscountCurrency",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ScheduledDate",
                schema: "booking",
                table: "TourBookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                schema: "booking",
                table: "TourBookings",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotalPriceCurrency",
                schema: "booking",
                table: "TourBookings",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_ConfirmationCode",
                schema: "booking",
                table: "TourBookings",
                column: "ConfirmationCode",
                unique: true,
                filter: "[ConfirmationCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TourBookings_UserId_ScheduledDate",
                schema: "booking",
                table: "TourBookings",
                columns: new[] { "UserId", "ScheduledDate" });
        }
    }
}
