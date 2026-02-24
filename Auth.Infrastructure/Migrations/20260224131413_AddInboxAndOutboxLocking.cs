using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxAndOutboxLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                schema: "auth",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "auth");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                schema: "auth",
                table: "OutboxMessages");
        }
    }
}
