using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Security.Infrastructure.Migrations
{
    /// <summary>
    /// Phase 4 — adds three nullable columns to <c>security.AuditLogs</c>
    /// to support the admin audit timeline:
    /// <list type="bullet">
    ///   <item><description><c>ActorUserId</c> — the admin who performed the action.</description></item>
    ///   <item><description><c>Reason</c> — admin-facing free-text reason (≤500 chars).</description></item>
    ///   <item><description><c>Metadata</c> — compact JSON describing the mutation.</description></item>
    /// </list>
    /// Adds composite index <c>IX_AuditLogs_ActorUserId_OccurredAt</c>
    /// so admin-by-admin queries stay cheap.
    /// All new columns are nullable: existing audit rows
    /// (REGISTER, LOGIN, LOGOUT, PASSWORD_CHANGED, PASSWORD_RESET) and
    /// their handlers continue to work unchanged.
    /// </summary>
    public partial class AddAdminAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                schema: "security",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                schema: "security",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                schema: "security",
                table: "AuditLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId_OccurredAt",
                schema: "security",
                table: "AuditLogs",
                columns: new[] { "ActorUserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActorUserId_OccurredAt",
                schema: "security",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                schema: "security",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Metadata",
                schema: "security",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                schema: "security",
                table: "AuditLogs");
        }
    }
}
