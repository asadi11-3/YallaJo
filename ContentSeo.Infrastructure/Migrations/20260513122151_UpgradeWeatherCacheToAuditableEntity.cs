using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentSeo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeWeatherCacheToAuditableEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "content_seo",
                table: "WeatherCache",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "content_seo",
                table: "WeatherCache",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "content_seo",
                table: "WeatherCache",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "content_seo",
                table: "WeatherCache");
        }
    }
}
