using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TranslationCaches",
                schema: "content_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TranslatedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromLanguage = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ToLanguage = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    EntityType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FieldName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationCaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TranslationCache_Entity",
                schema: "content_core",
                table: "TranslationCaches",
                columns: new[] { "EntityType", "EntityId" },
                filter: "[EntityType] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationCache_Lookup",
                schema: "content_core",
                table: "TranslationCaches",
                columns: new[] { "FromLanguage", "ToLanguage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranslationCaches",
                schema: "content_core");
        }
    }
}
