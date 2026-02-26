using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentBlogs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlaceId",
                schema: "content_blogs",
                table: "Blogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_PlaceId",
                schema: "content_blogs",
                table: "Blogs",
                column: "PlaceId",
                filter: "[PlaceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Blogs_PlaceId",
                schema: "content_blogs",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                schema: "content_blogs",
                table: "Blogs");
        }
    }
}
