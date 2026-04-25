using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecializationTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceLanguageCode",
                schema: "content_core",
                table: "Specializations",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.CreateTable(
                name: "SpecializationTranslations",
                schema: "content_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecializationTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecializationTranslations_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalSchema: "content_core",
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecializationTranslations_LanguageId",
                schema: "content_core",
                table: "SpecializationTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "UX_SpecializationTranslations_SpecId_LanguageId",
                schema: "content_core",
                table: "SpecializationTranslations",
                columns: new[] { "SpecializationId", "LanguageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecializationTranslations",
                schema: "content_core");

            migrationBuilder.DropColumn(
                name: "SourceLanguageCode",
                schema: "content_core",
                table: "Specializations");
        }
    }
}
