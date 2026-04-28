using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTourScheduleTimeOnlyConversionAndIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourSchedules_TourId",
                schema: "content_tours",
                table: "TourSchedules");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                schema: "content_tours",
                table: "TourSchedules",
                type: "time(7)",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                schema: "content_tours",
                table: "TourSchedules",
                type: "time(7)",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TourId_DayOfWeek_StartTime",
                schema: "content_tours",
                table: "TourSchedules",
                columns: new[] { "TourId", "DayOfWeek", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TourSchedules_TourId_DayOfWeek_StartTime",
                schema: "content_tours",
                table: "TourSchedules");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "StartTime",
                schema: "content_tours",
                table: "TourSchedules",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time(7)");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "EndTime",
                schema: "content_tours",
                table: "TourSchedules",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time(7)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TourSchedules_TourId",
                schema: "content_tours",
                table: "TourSchedules",
                column: "TourId");
        }
    }
}
