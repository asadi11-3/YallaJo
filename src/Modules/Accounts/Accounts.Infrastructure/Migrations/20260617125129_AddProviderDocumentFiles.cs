using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderDocumentFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderDocumentFiles",
                schema: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderDocumentFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderDocumentFiles_ProviderDocuments_ProviderDocumentId",
                        column: x => x.ProviderDocumentId,
                        principalSchema: "accounts",
                        principalTable: "ProviderDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocumentFiles_FileAssetId",
                schema: "accounts",
                table: "ProviderDocumentFiles",
                column: "FileAssetId");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderDocumentFiles_ProviderDocumentId",
                schema: "accounts",
                table: "ProviderDocumentFiles",
                column: "ProviderDocumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderDocumentFiles",
                schema: "accounts");
        }
    }
}
