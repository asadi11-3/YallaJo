using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReferredRewardAmount",
                schema: "finance",
                table: "Referrals",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                schema: "finance",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                schema: "finance",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DaysBeforeTour",
                schema: "finance",
                table: "Discounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsesPerUser",
                schema: "finance",
                table: "Discounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                schema: "finance",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceItemId",
                schema: "finance",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetScope",
                schema: "finance",
                table: "Discounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TourId",
                schema: "finance",
                table: "Discounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                schema: "finance",
                table: "Discounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DisputeEvidence",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeEvidence_Disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalSchema: "finance",
                        principalTable: "Disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisputeMessages",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeMessages_Disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalSchema: "finance",
                        principalTable: "Disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderBankAccounts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Iban = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    SwiftCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderBankAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ProviderId",
                schema: "finance",
                table: "Discounts",
                column: "ProviderId",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_TourId",
                schema: "finance",
                table: "Discounts",
                column: "TourId",
                filter: "[TourId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeEvidence_DisputeId",
                schema: "finance",
                table: "DisputeEvidence",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_DisputeId",
                schema: "finance",
                table: "DisputeMessages",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderBankAccounts_UserId",
                schema: "finance",
                table: "ProviderBankAccounts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisputeEvidence",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "DisputeMessages",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "ProviderBankAccounts",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_ProviderId",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_TourId",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "ReferredRewardAmount",
                schema: "finance",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "DaysBeforeTour",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "MaxUsesPerUser",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "ServiceItemId",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "TargetScope",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "TourId",
                schema: "finance",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "finance",
                table: "Discounts");
        }
    }
}
