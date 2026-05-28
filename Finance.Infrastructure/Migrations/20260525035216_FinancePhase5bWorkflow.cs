using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinancePhase5bWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAt",
                schema: "finance",
                table: "Disputes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscalatedByAdminId",
                schema: "finance",
                table: "Disputes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                schema: "finance",
                table: "Disputes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                schema: "finance",
                table: "Disputes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByAdminId",
                schema: "finance",
                table: "Disputes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderPaymentMethods",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethodType = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AccountIdentifier = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderPaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_Status_CreatedAt",
                schema: "finance",
                table: "Disputes",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPaymentMethods_UserId",
                schema: "finance",
                table: "ProviderPaymentMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPaymentMethods_UserId_AccountIdentifier",
                schema: "finance",
                table: "ProviderPaymentMethods",
                columns: new[] { "UserId", "AccountIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPaymentMethods_UserId_IsDefault",
                schema: "finance",
                table: "ProviderPaymentMethods",
                columns: new[] { "UserId", "IsDefault" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderPaymentMethods",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_Status_CreatedAt",
                schema: "finance",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                schema: "finance",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "EscalatedByAdminId",
                schema: "finance",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                schema: "finance",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "finance",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                schema: "finance",
                table: "Disputes");
        }
    }
}
