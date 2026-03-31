using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditFixes_RowVersions_UniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryTranslations_CategoryId",
                schema: "content_core",
                table: "CategoryTranslations");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "content_core",
                table: "TranslationCaches",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "content_core",
                table: "Tags",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "UX_CategoryTranslations_CategoryId_LanguageId",
                schema: "content_core",
                table: "CategoryTranslations",
                columns: new[] { "CategoryId", "LanguageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CategoryTranslations_CategoryId_LanguageId",
                schema: "content_core",
                table: "CategoryTranslations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "content_core",
                table: "TranslationCaches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "content_core",
                table: "Tags");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTranslations_CategoryId",
                schema: "content_core",
                table: "CategoryTranslations",
                column: "CategoryId");
        }
    }
}
