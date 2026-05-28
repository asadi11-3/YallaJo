using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentPlaces.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContentPlacesWorkflowChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PlaceId",
                schema: "content_places",
                table: "Businesses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentExpiryDate",
                schema: "content_places",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodEnd",
                schema: "content_places",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAlcoholFreeArea",
                schema: "content_places",
                table: "Businesses",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasVegetarianOptions",
                schema: "content_places",
                table: "Businesses",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHalal",
                schema: "content_places",
                table: "Businesses",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResubmitCount",
                schema: "content_places",
                table: "Businesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDeadline",
                schema: "content_places",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                schema: "content_places",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Places_Name_Country",
                schema: "content_places",
                table: "Places",
                columns: new[] { "Name", "Country" },
                unique: true,
                filter: "[Country] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_LicenseNumber_BusinessType",
                schema: "content_places",
                table: "Businesses",
                columns: new[] { "LicenseNumber", "BusinessType" },
                unique: true,
                filter: "[LicenseNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_PlaceId_BusinessType_OwnerId",
                schema: "content_places",
                table: "Businesses",
                columns: new[] { "PlaceId", "BusinessType", "OwnerId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Places_Name_Country",
                schema: "content_places",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_LicenseNumber_BusinessType",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Businesses_PlaceId_BusinessType_OwnerId",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "DocumentExpiryDate",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "GracePeriodEnd",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "HasAlcoholFreeArea",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "HasVegetarianOptions",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "IsHalal",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ResubmitCount",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ReviewDeadline",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                schema: "content_places",
                table: "Businesses");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlaceId",
                schema: "content_places",
                table: "Businesses",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
