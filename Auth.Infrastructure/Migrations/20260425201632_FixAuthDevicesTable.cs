using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAuthDevicesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Otps_IsUsed",
                schema: "auth",
                table: "Otps");

            migrationBuilder.DropIndex(
                name: "IX_Otps_UserId",
                schema: "auth",
                table: "Otps");

            migrationBuilder.DropIndex(
                name: "IX_ExternalProviders_Provider_UserId",
                schema: "auth",
                table: "ExternalProviders");

            migrationBuilder.CreateIndex(
                name: "IX_Otps_UserId_Purpose_IsUsed_Active",
                schema: "auth",
                table: "Otps",
                columns: new[] { "UserId", "Purpose", "IsUsed" },
                filter: "[IsUsed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalProviders_Provider_ProviderUserId_Active",
                schema: "auth",
                table: "ExternalProviders",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Otps_UserId_Purpose_IsUsed_Active",
                schema: "auth",
                table: "Otps");

            migrationBuilder.DropIndex(
                name: "IX_ExternalProviders_Provider_ProviderUserId_Active",
                schema: "auth",
                table: "ExternalProviders");

            migrationBuilder.CreateIndex(
                name: "IX_Otps_IsUsed",
                schema: "auth",
                table: "Otps",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_Otps_UserId",
                schema: "auth",
                table: "Otps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalProviders_Provider_UserId",
                schema: "auth",
                table: "ExternalProviders",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);
        }
    }
}
