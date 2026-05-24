using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundPolicyTourScopeAndCommissionSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefundPolicies_IsDefault",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "FullRefundHours",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "PartialRefundHours",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "PartialRefundPercent",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.AddColumn<string>(
                name: "Tiers",
                schema: "booking",
                table: "RefundPolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TourId",
                schema: "booking",
                table: "RefundPolicies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CommissionSnapshots",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundPolicies_TourId",
                schema: "booking",
                table: "RefundPolicies",
                column: "TourId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionSnapshots_IsActive_Tier_Currency",
                schema: "booking",
                table: "CommissionSnapshots",
                columns: new[] { "IsActive", "Tier", "Currency" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAt",
                schema: "booking",
                table: "InboxMessages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionSnapshots",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "booking");

            migrationBuilder.DropIndex(
                name: "IX_RefundPolicies_TourId",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "Tiers",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.DropColumn(
                name: "TourId",
                schema: "booking",
                table: "RefundPolicies");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "booking",
                table: "RefundPolicies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FullRefundHours",
                schema: "booking",
                table: "RefundPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                schema: "booking",
                table: "RefundPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "booking",
                table: "RefundPolicies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PartialRefundHours",
                schema: "booking",
                table: "RefundPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PartialRefundPercent",
                schema: "booking",
                table: "RefundPolicies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_RefundPolicies_IsDefault",
                schema: "booking",
                table: "RefundPolicies",
                column: "IsDefault");
        }
    }
}
