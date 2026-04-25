using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <summary>
    /// Phase 2C-2 — introduces the dedicated <c>auth.PasswordResetTokens</c>
    /// table that replaces the overloaded <c>Otp(Purpose="PasswordReset")</c>
    /// representation. Additive migration: existing <c>Otp</c> reset rows
    /// are left in place so in-flight reset codes keep working via the
    /// <c>ResetPasswordCommand</c> fallback path. A later phase can
    /// purge stale <c>PasswordReset</c> rows once the 10-minute window
    /// has drained after deploy.
    /// </summary>
    public partial class CreatePasswordResetTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
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
                    ResetOrigin = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_ExpiresAt",
                schema: "auth",
                table: "PasswordResetTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId_State",
                schema: "auth",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "State" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetTokens",
                schema: "auth");
        }
    }
}
