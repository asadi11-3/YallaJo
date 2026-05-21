using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceT1ThroughT6Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "EntityType",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "MaxAmount",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "MaxAmountCurrency",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.RenameColumn(
                name: "MinAmountCurrency",
                schema: "finance",
                table: "CommissionRules",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "MinAmount",
                schema: "finance",
                table: "CommissionRules",
                newName: "MinMonthlyRevenue");

            migrationBuilder.RenameColumn(
                name: "CommissionPercentage",
                schema: "finance",
                table: "CommissionRules",
                newName: "Percentage");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                schema: "finance",
                table: "Payouts",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "finance",
                table: "Payouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                schema: "finance",
                table: "Payouts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                schema: "finance",
                table: "Payouts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BatchPeriodEnd",
                schema: "finance",
                table: "Payouts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "BatchPeriodStart",
                schema: "finance",
                table: "Payouts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionAmount",
                schema: "finance",
                table: "Payouts",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CommissionAmountCurrency",
                schema: "finance",
                table: "Payouts",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "finance",
                table: "Payouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "finance",
                table: "Payouts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayPayoutId",
                schema: "finance",
                table: "Payouts",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                schema: "finance",
                table: "Payouts",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GrossAmountCurrency",
                schema: "finance",
                table: "Payouts",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD");

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                schema: "finance",
                table: "Payouts",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NetAmountCurrency",
                schema: "finance",
                table: "Payouts",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD");

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                schema: "finance",
                table: "Payouts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CommissionRuleSnapshotId",
                schema: "finance",
                table: "PayoutItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                schema: "finance",
                table: "Payments",
                type: "varchar(500)",
                unicode: false,
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscrowReleaseEligibleAt",
                schema: "finance",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "finance",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayProvider",
                schema: "finance",
                table: "Payments",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GatewayTransactionId",
                schema: "finance",
                table: "Payments",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalPaymentId",
                schema: "finance",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PaymentType",
                schema: "finance",
                table: "Payments",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                schema: "finance",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "RecipientAccount",
                schema: "finance",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "platform-escrow");

            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                schema: "finance",
                table: "Payments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedTotal",
                schema: "finance",
                table: "Payments",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RefundedTotalCurrency",
                schema: "finance",
                table: "Payments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD");

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "finance",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "finance",
                table: "CommissionRules",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldDefaultValue: "JOD");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxMonthlyRevenue",
                schema: "finance",
                table: "CommissionRules",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "finance",
                table: "CommissionRules",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                schema: "finance",
                table: "CommissionRules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentExpectations",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TourId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    ExpectedAmountCurrency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false, defaultValue: "JOD"),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false, defaultValue: "JOD"),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentExpectations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_BatchPeriodStart_BatchPeriodEnd",
                schema: "finance",
                table: "Payouts",
                columns: new[] { "BatchPeriodStart", "BatchPeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ProviderId_Status",
                schema: "finance",
                table: "Payouts",
                columns: new[] { "ProviderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_Status",
                schema: "finance",
                table: "Payouts",
                column: "Status",
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutItems_BookingId",
                schema: "finance",
                table: "PayoutItems",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId_Status",
                schema: "finance",
                table: "Payments",
                columns: new[] { "BookingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_EscrowReleaseEligibleAt",
                schema: "finance",
                table: "Payments",
                column: "EscrowReleaseEligibleAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayTransactionId",
                schema: "finance",
                table: "Payments",
                column: "GatewayTransactionId",
                unique: true,
                filter: "[GatewayTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentType_Status_UpdatedAt",
                schema: "finance",
                table: "Payments",
                columns: new[] { "PaymentType", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderId_Status",
                schema: "finance",
                table: "Payments",
                columns: new[] { "ProviderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_Tier_Currency",
                schema: "finance",
                table: "CommissionRules",
                columns: new[] { "Tier", "Currency" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_Tier_Currency_MinMonthlyRevenue_MaxMonthlyRevenue",
                schema: "finance",
                table: "CommissionRules",
                columns: new[] { "Tier", "Currency", "MinMonthlyRevenue", "MaxMonthlyRevenue" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentExpectations_BookingId",
                schema: "finance",
                table: "PaymentExpectations",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentExpectations_ProviderId",
                schema: "finance",
                table: "PaymentExpectations",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentExpectations_UserId",
                schema: "finance",
                table: "PaymentExpectations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "PaymentExpectations",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_BatchPeriodStart_BatchPeriodEnd",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_ProviderId_Status",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_Payouts_Status",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_PayoutItems_BookingId",
                schema: "finance",
                table: "PayoutItems");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BookingId_Status",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_EscrowReleaseEligibleAt",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_GatewayTransactionId",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentType_Status_UpdatedAt",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderId_Status",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRules_Tier_Currency",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRules_Tier_Currency_MinMonthlyRevenue_MaxMonthlyRevenue",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "BatchPeriodEnd",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "BatchPeriodStart",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "CommissionAmount",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "CommissionAmountCurrency",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "GatewayPayoutId",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "GrossAmountCurrency",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "NetAmountCurrency",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "finance",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "CommissionRuleSnapshotId",
                schema: "finance",
                table: "PayoutItems");

            migrationBuilder.DropColumn(
                name: "ClientSecret",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EscrowReleaseEligibleAt",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayProvider",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "GatewayTransactionId",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OriginalPaymentId",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RecipientAccount",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundedTotal",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundedTotalCurrency",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "finance",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "MaxMonthlyRevenue",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "Tier",
                schema: "finance",
                table: "CommissionRules");

            migrationBuilder.RenameColumn(
                name: "Currency",
                schema: "finance",
                table: "CommissionRules",
                newName: "MinAmountCurrency");

            migrationBuilder.RenameColumn(
                name: "Percentage",
                schema: "finance",
                table: "CommissionRules",
                newName: "CommissionPercentage");

            migrationBuilder.RenameColumn(
                name: "MinMonthlyRevenue",
                schema: "finance",
                table: "CommissionRules",
                newName: "MinAmount");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "finance",
                table: "Payouts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)0);

            migrationBuilder.AlterColumn<string>(
                name: "MinAmountCurrency",
                schema: "finance",
                table: "CommissionRules",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD",
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldUnicode: false,
                oldMaxLength: 3);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "finance",
                table: "CommissionRules",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                schema: "finance",
                table: "CommissionRules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxAmount",
                schema: "finance",
                table: "CommissionRules",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MaxAmountCurrency",
                schema: "finance",
                table: "CommissionRules",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "finance",
                table: "CommissionRules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "finance",
                table: "CommissionRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                schema: "finance",
                table: "CommissionRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                schema: "finance",
                table: "CommissionRules",
                type: "datetime2",
                nullable: true);
        }
    }
}
