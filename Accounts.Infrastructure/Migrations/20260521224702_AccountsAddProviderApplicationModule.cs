using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountsAddProviderApplicationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderApplications",
                schema: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReapplicationCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CoolingPeriodEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TypeSpecificDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderDocuments",
                schema: "accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderDocuments_ProviderApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "accounts",
                        principalTable: "ProviderApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderApplications_Status_ReviewedAt",
                schema: "accounts",
                table: "ProviderApplications",
                columns: new[] { "Status", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderApplications_Type_Status",
                schema: "accounts",
                table: "ProviderApplications",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderApplications_UserId",
                schema: "accounts",
                table: "ProviderApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderDocuments_ApplicationId",
                schema: "accounts",
                table: "ProviderDocuments",
                column: "ApplicationId");

            // Unique filtered index: only one approved provider per user
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX [UIX_ProviderApplications_UserId_Approved]
                ON [accounts].[ProviderApplications] ([UserId])
                WHERE [Status] = 'Approved' AND [IsDeleted] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS [UIX_ProviderApplications_UserId_Approved] ON [accounts].[ProviderApplications];");

            migrationBuilder.DropTable(
                name: "ProviderDocuments",
                schema: "accounts");

            migrationBuilder.DropTable(
                name: "ProviderApplications",
                schema: "accounts");
        }
    }
}
