using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <summary>
    /// Phase 2C-1 — introduces the dedicated <c>auth.ActivationTokens</c>
    /// table that replaces the overloaded <c>Otp(Purpose="UserInvite")</c>
    /// representation. Additive migration: existing <c>Otp</c> invite rows
    /// are left in place so in-flight activation links keep working via
    /// the <c>ActivateAccountCommand</c> fallback path. A later phase
    /// (2C-2) can purge stale <c>UserInvite</c> rows once the 7-day
    /// activation window has drained.
    /// </summary>
    public partial class CreateActivationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivationTokens",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedReason = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DeliveryStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationTokens_ExpiresAt",
                schema: "auth",
                table: "ActivationTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationTokens_UserId_State",
                schema: "auth",
                table: "ActivationTokens",
                columns: new[] { "UserId", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationTokens",
                schema: "auth");
        }
    }
}
