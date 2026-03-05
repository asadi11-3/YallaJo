using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                schema: "accounts",
                table: "Profiles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "accounts",
                table: "Profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "accounts",
                table: "Profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                schema: "accounts",
                table: "Profiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                schema: "accounts",
                table: "Profiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "accounts",
                table: "Profiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                schema: "accounts",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "accounts",
                table: "Profiles");
        }
    }
}
