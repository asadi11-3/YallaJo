using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourPackageApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "content_tours",
                table: "TourPackages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                schema: "content_tours",
                table: "TourPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByAdminId",
                schema: "content_tours",
                table: "TourPackages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                schema: "content_tours",
                table: "TourPackages",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                schema: "content_tours",
                table: "TourPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_Status",
                schema: "content_tours",
                table: "TourPackages",
                column: "Status");

            // WS-5a (Phase 3 G3a) — backfill: pre-existing packages were already
            // in use under the old IsActive-only flag; mark them Approved so they
            // remain visible. New rows still default to Draft via the entity
            // initializer (TourPackage.Status = TourPackageStatus.Draft).
            migrationBuilder.Sql(
                "UPDATE [content_tours].[TourPackages] SET [Status] = 2 WHERE [Status] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourPackages_Status",
                schema: "content_tours",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "content_tours",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "content_tours",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                schema: "content_tours",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "content_tours",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                schema: "content_tours",
                table: "TourPackages");
        }
    }
}
