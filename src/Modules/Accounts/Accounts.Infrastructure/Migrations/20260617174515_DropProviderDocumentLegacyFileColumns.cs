using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropProviderDocumentLegacyFileColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "accounts",
                table: "ProviderDocuments");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                schema: "accounts",
                table: "ProviderDocuments");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                schema: "accounts",
                table: "ProviderDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "accounts",
                table: "ProviderDocuments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                schema: "accounts",
                table: "ProviderDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                schema: "accounts",
                table: "ProviderDocuments",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");
        }
    }
}
