using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentPlaces.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Place_AddCategoryIdAndTourCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                schema: "content_places",
                table: "Places",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TourCount",
                schema: "content_places",
                table: "Places",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Places_CategoryId",
                schema: "content_places",
                table: "Places",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_TourCount",
                schema: "content_places",
                table: "Places",
                column: "TourCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Places_CategoryId",
                schema: "content_places",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_TourCount",
                schema: "content_places",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "content_places",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "TourCount",
                schema: "content_places",
                table: "Places");
        }
    }
}
