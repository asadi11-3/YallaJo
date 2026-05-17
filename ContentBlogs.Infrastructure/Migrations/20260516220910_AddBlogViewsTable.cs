using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentBlogs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogViewsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogViews",
                schema: "content_blogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewerHash = table.Column<byte[]>(type: "binary(32)", maxLength: 32, nullable: false),
                    ViewerKind = table.Column<byte>(type: "tinyint", nullable: false),
                    ViewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogViews_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalSchema: "content_blogs",
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogViews_BlogId_ViewerHash_Unique",
                schema: "content_blogs",
                table: "BlogViews",
                columns: new[] { "BlogId", "ViewerHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogViews",
                schema: "content_blogs");
        }
    }
}
