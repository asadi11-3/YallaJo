using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentSeo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Redirects",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    NewUrl = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HitCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Redirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SitemapEntries",
                schema: "content_seo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "varchar(2048)", unicode: false, maxLength: 2048, nullable: false),
                    ChangeFrequency = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Priority = table.Column<decimal>(type: "decimal(2,1)", precision: 2, scale: 1, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SitemapEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Redirects_OldUrl",
                schema: "content_seo",
                table: "Redirects",
                column: "OldUrl",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SitemapEntries_EntityType_EntityId",
                schema: "content_seo",
                table: "SitemapEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SitemapEntries_Url",
                schema: "content_seo",
                table: "SitemapEntries",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Redirects",
                schema: "content_seo");

            migrationBuilder.DropTable(
                name: "SitemapEntries",
                schema: "content_seo");
        }
    }
}
