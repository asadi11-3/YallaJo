using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceAddInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLineItems",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_InvoiceNumber",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DueDate",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxAmountCurrency",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.RenameColumn(
                name: "SubTotalCurrency",
                schema: "finance",
                table: "InvoiceItems",
                newName: "SubtotalCurrency");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                schema: "finance",
                table: "InvoiceItems",
                newName: "Subtotal");

            migrationBuilder.RenameColumn(
                name: "TotalAmountCurrency",
                schema: "finance",
                table: "InvoiceItems",
                newName: "UnitPriceCurrency");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                schema: "finance",
                table: "InvoiceItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "finance",
                table: "InvoiceItems",
                newName: "InvoiceId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "finance",
                table: "InvoiceItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                schema: "finance",
                table: "InvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    AmountSubtotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AmountSubtotalCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    AmountTax = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AmountTaxCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    AmountDiscount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AmountDiscountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    AmountTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AmountTotalCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BuyerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BuyerEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    SellerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SellerTaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PdfStoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                schema: "finance",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BookingId",
                schema: "finance",
                table: "Invoices",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                schema: "finance",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentId",
                schema: "finance",
                table: "Invoices",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ProviderId_IssuedAt",
                schema: "finance",
                table: "Invoices",
                columns: new[] { "ProviderId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UserId_IssuedAt",
                schema: "finance",
                table: "Invoices",
                columns: new[] { "UserId", "IssuedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Invoices_InvoiceId",
                schema: "finance",
                table: "InvoiceItems",
                column: "InvoiceId",
                principalSchema: "finance",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Invoices_InvoiceId",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_InvoiceId",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "finance",
                table: "InvoiceItems");

            migrationBuilder.RenameColumn(
                name: "SubtotalCurrency",
                schema: "finance",
                table: "InvoiceItems",
                newName: "SubTotalCurrency");

            migrationBuilder.RenameColumn(
                name: "Subtotal",
                schema: "finance",
                table: "InvoiceItems",
                newName: "SubTotal");

            migrationBuilder.RenameColumn(
                name: "UnitPriceCurrency",
                schema: "finance",
                table: "InvoiceItems",
                newName: "TotalAmountCurrency");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                schema: "finance",
                table: "InvoiceItems",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "InvoiceId",
                schema: "finance",
                table: "InvoiceItems",
                newName: "UserId");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "finance",
                table: "InvoiceItems",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "finance",
                table: "InvoiceItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                schema: "finance",
                table: "InvoiceItems",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                schema: "finance",
                table: "InvoiceItems",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "finance",
                table: "InvoiceItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "finance",
                table: "InvoiceItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                schema: "finance",
                table: "InvoiceItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "finance",
                table: "InvoiceItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "finance",
                table: "InvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "finance",
                table: "InvoiceItems",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TaxAmountCurrency",
                schema: "finance",
                table: "InvoiceItems",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "JOD");

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AmountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD"),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "JOD")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_InvoiceItems_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "finance",
                        principalTable: "InvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceNumber",
                schema: "finance",
                table: "InvoiceItems",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                schema: "finance",
                table: "InvoiceLineItems",
                column: "InvoiceId");
        }
    }
}
