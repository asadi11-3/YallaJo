using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModelBooking2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderDocuments_TourGuideId_DocumentType",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionDispatchedAt",
                schema: "booking",
                table: "ProviderDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_Suspension_Pending",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "Status", "DocumentType", "SuspensionDispatchedAt" },
                filter: "[Status] = 3 AND [SuspensionDispatchedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderDocuments_Business_Type_Active",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "BusinessId", "DocumentType" },
                unique: true,
                filter: "[BusinessId] IS NOT NULL AND [Status] <> 2 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderDocuments_TourGuide_Type_Active",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "TourGuideId", "DocumentType" },
                unique: true,
                filter: "[TourGuideId] IS NOT NULL AND [Status] <> 2 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderDocuments_Suspension_Pending",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropIndex(
                name: "UX_ProviderDocuments_Business_Type_Active",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropIndex(
                name: "UX_ProviderDocuments_TourGuide_Type_Active",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.DropColumn(
                name: "SuspensionDispatchedAt",
                schema: "booking",
                table: "ProviderDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_TourGuideId_DocumentType",
                schema: "booking",
                table: "ProviderDocuments",
                columns: new[] { "TourGuideId", "DocumentType" });
        }
    }
}
