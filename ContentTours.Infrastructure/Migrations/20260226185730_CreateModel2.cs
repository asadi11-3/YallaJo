using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentTours.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgeRestriction",
                schema: "content_tours",
                table: "Tours",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                schema: "content_tours",
                table: "Tours",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountValidFrom",
                schema: "content_tours",
                table: "Tours",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountValidTo",
                schema: "content_tours",
                table: "Tours",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAccessible",
                schema: "content_tours",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsChildFriendly",
                schema: "content_tours",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PlaceId",
                schema: "content_tours",
                table: "Tours",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                schema: "content_tours",
                table: "Tours",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalePriceCurrency",
                schema: "content_tours",
                table: "Tours",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_PlaceId",
                schema: "content_tours",
                table: "Tours",
                column: "PlaceId",
                filter: "[PlaceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tours_PlaceId",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "AgeRestriction",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "DiscountValidFrom",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "DiscountValidTo",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsAccessible",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsChildFriendly",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                schema: "content_tours",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "SalePriceCurrency",
                schema: "content_tours",
                table: "Tours");
        }
    }
}
