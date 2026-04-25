using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceLanguageCode",
                schema: "content_core",
                table: "Tags",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateTable(
                name: "TagTranslations",
                schema: "content_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagTranslations_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "content_core",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagTranslations_LanguageId",
                schema: "content_core",
                table: "TagTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "UX_TagTranslations_TagId_LanguageId",
                schema: "content_core",
                table: "TagTranslations",
                columns: new[] { "TagId", "LanguageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagTranslations",
                schema: "content_core");

            migrationBuilder.DropColumn(
                name: "SourceLanguageCode",
                schema: "content_core",
                table: "Tags");
        }
    }
}
