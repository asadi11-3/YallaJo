using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentSeo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WeatherDailyBudgetAndCoordinateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeatherCache_PlaceId_FetchedAt",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.RenameColumn(
                name: "Forecast",
                schema: "content_seo",
                table: "WeatherCache",
                newName: "ForecastJson");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlaceId",
                schema: "content_seo",
                table: "WeatherCache",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "content_seo",
                table: "WeatherCache",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ForecastDate",
                schema: "content_seo",
                table: "WeatherCache",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "RoundedLatitude",
                schema: "content_seo",
                table: "WeatherCache",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundedLongitude",
                schema: "content_seo",
                table: "WeatherCache",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Question",
                schema: "content_seo",
                table: "FaqItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateTable(
                name: "WeatherDailyBudget",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CallsUsed = table.Column<int>(type: "int", nullable: false),
                    DailyLimit = table.Column<int>(type: "int", nullable: false),
                    AlertSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherDailyBudget", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherCache_ExpiresAt",
                schema: "content_seo",
                table: "WeatherCache",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherCache_PlaceId",
                schema: "content_seo",
                table: "WeatherCache",
                column: "PlaceId",
                filter: "[PlaceId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_WeatherCache_Lat_Lng_Date",
                schema: "content_seo",
                table: "WeatherCache",
                columns: new[] { "RoundedLatitude", "RoundedLongitude", "ForecastDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_WeatherDailyBudget_Date",
                schema: "content_seo",
                table: "WeatherDailyBudget",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherDailyBudget",
                schema: "content_seo");

            migrationBuilder.DropIndex(
                name: "IX_WeatherCache_ExpiresAt",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.DropIndex(
                name: "IX_WeatherCache_PlaceId",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.DropIndex(
                name: "UX_WeatherCache_Lat_Lng_Date",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.DropColumn(
                name: "ForecastDate",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.DropColumn(
                name: "RoundedLatitude",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.DropColumn(
                name: "RoundedLongitude",
                schema: "content_seo",
                table: "WeatherCache");

            migrationBuilder.RenameColumn(
                name: "ForecastJson",
                schema: "content_seo",
                table: "WeatherCache",
                newName: "Forecast");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlaceId",
                schema: "content_seo",
                table: "WeatherCache",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "content_seo",
                table: "WeatherCache",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Question",
                schema: "content_seo",
                table: "FaqItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherCache_PlaceId_FetchedAt",
                schema: "content_seo",
                table: "WeatherCache",
                columns: new[] { "PlaceId", "FetchedAt" });
        }
    }
}
