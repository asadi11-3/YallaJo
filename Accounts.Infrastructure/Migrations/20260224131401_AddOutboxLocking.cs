using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                schema: "accounts",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedUntil",
                schema: "accounts",
                table: "OutboxMessages");
        }
    }
}
