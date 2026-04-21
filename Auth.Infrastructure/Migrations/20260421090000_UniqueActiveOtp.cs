using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Infrastructure.Migrations;

/// <inheritdoc />
public partial class UniqueActiveOtp : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the old non-unique covering index on (UserId, Purpose, IsUsed)
        // filtered by [IsUsed] = 0. We replace it with a narrower UNIQUE filtered
        // index on (UserId, Purpose) that additionally filters out soft-deleted
        // rows, so rejected OTPs no longer have to be hard-deleted to make room.
        migrationBuilder.DropIndex(
            name: "IX_Otps_UserId_Purpose_IsUsed_Active",
            schema: "auth",
            table: "Otps");

        // Reconcile any existing data BEFORE creating the unique index. Multiple
        // active OTPs per (UserId, Purpose) are possible on older databases
        // (before this migration). Mark everything but the most recently created
        // active OTP as used so the new unique index can be created.
        migrationBuilder.Sql(@"
WITH Ranked AS (
    SELECT Id,
           ROW_NUMBER() OVER (
               PARTITION BY UserId, Purpose
               ORDER BY CreatedAt DESC, Id DESC
           ) AS rn
    FROM [auth].[Otps]
    WHERE IsUsed = 0 AND IsDeleted = 0
)
UPDATE o
SET    IsUsed = 1,
       UsedAt = SYSUTCDATETIME(),
       UpdatedAt = SYSUTCDATETIME()
FROM   [auth].[Otps] o
JOIN   Ranked r ON r.Id = o.Id
WHERE  r.rn > 1;
");

        migrationBuilder.CreateIndex(
            name: "IX_Otps_UserId_Purpose_Active_Unique",
            schema: "auth",
            table: "Otps",
            columns: new[] { "UserId", "Purpose" },
            unique: true,
            filter: "[IsUsed] = 0 AND [IsDeleted] = 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Otps_UserId_Purpose_Active_Unique",
            schema: "auth",
            table: "Otps");

        migrationBuilder.CreateIndex(
            name: "IX_Otps_UserId_Purpose_IsUsed_Active",
            schema: "auth",
            table: "Otps",
            columns: new[] { "UserId", "Purpose", "IsUsed" },
            filter: "[IsUsed] = 0");
    }
}
