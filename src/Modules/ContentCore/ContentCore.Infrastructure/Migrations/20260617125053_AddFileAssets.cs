using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileAssets",
                schema: "content_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageProvider = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(1024)", unicode: false, maxLength: 1024, nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    SafeFileName = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Extension = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAssets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileAssets_Sha256",
                schema: "content_core",
                table: "FileAssets",
                column: "Sha256",
                filter: "[Sha256] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FileAssets_StorageKey",
                schema: "content_core",
                table: "FileAssets",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileAssets",
                schema: "content_core");
        }
    }
}
