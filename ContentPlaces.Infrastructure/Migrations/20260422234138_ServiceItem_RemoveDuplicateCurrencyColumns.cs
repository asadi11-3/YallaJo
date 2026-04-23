using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentPlaces.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServiceItem_RemoveDuplicateCurrencyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceCurrency",
                schema: "content_places",
                table: "ServiceItems");

            migrationBuilder.DropColumn(
                name: "SalePriceCurrency",
                schema: "content_places",
                table: "ServiceItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriceCurrency",
                schema: "content_places",
                table: "ServiceItems",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalePriceCurrency",
                schema: "content_places",
                table: "ServiceItems",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);
        }
    }
}
