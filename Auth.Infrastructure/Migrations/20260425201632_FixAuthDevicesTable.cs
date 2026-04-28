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
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.objects o ON o.object_id = i.object_id
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = 'auth' AND o.name = 'Otps' AND i.name = 'IX_Otps_IsUsed'
)
    DROP INDEX [IX_Otps_IsUsed] ON [auth].[Otps];");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.objects o ON o.object_id = i.object_id
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = 'auth' AND o.name = 'Otps' AND i.name = 'IX_Otps_UserId'
)
    DROP INDEX [IX_Otps_UserId] ON [auth].[Otps];");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.objects o ON o.object_id = i.object_id
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = 'auth' AND o.name = 'ExternalProviders' AND i.name = 'IX_ExternalProviders_Provider_UserId'
)
    DROP INDEX [IX_ExternalProviders_Provider_UserId] ON [auth].[ExternalProviders];");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.objects o ON o.object_id = i.object_id
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = 'auth' AND o.name = 'Otps' AND i.name = 'IX_Otps_UserId_Purpose_IsUsed_Active'
)
    CREATE INDEX [IX_Otps_UserId_Purpose_IsUsed_Active]
    ON [auth].[Otps] ([UserId], [Purpose], [IsUsed])
    WHERE [IsUsed] = 0;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.objects o ON o.object_id = i.object_id
    JOIN sys.schemas s ON s.schema_id = o.schema_id
    WHERE s.name = 'auth' AND o.name = 'ExternalProviders' AND i.name = 'IX_ExternalProviders_Provider_ProviderUserId_Active'
)
    CREATE UNIQUE INDEX [IX_ExternalProviders_Provider_ProviderUserId_Active]
    ON [auth].[ExternalProviders] ([Provider], [ProviderUserId])
    WHERE [IsActive] = 1;");
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
