using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Security.Infrastructure.Migrations
{
    /// <summary>
    /// Phase 2A — adds the explicit <c>LifecycleState</c> column on
    /// <c>security.Users</c> alongside the existing <c>IsActive</c> flag and
    /// backfills it deterministically from current data.
    ///
    /// <para>
    /// Backfill rules (executed in raw SQL after the column is added):
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>IsActive = 1</c> → <c>Active</c> (2). Established truth — the user is logging in today.</description></item>
    ///   <item><description><c>IsActive = 0</c> AND primary email is verified → <c>Active</c> (2). Data anomaly: a user whose primary email is verified but who is flagged inactive cannot exist in the new model without manual intervention. Promoting them to <c>Active</c> matches what every existing query already returns for them (the <c>VerifyEmail</c> domain method always activates the account in lockstep). If the deployment includes accounts that were intentionally deactivated AFTER verification, operators must run a one-off post-deploy SQL to revert those rows to <c>Suspended</c> (3).</description></item>
    ///   <item><description><c>IsActive = 0</c> AND <c>PasswordHash</c> begins with <c>EXTERNAL-ONLY:</c> → <c>Active</c> (2). External-only users created via <c>RegisterExternalAsync</c> are fully onboarded; their inert hash placeholder exists only because the schema requires <c>NOT NULL</c>.</description></item>
    ///   <item><description><c>IsActive = 0</c> AND any unconsumed <c>UserInvite</c> OTP exists in <c>auth.Otps</c> for the user → <c>PendingActivation</c> (1). The admin sent the invite and the user has not yet activated.</description></item>
    ///   <item><description><c>IsActive = 0</c> AND no outstanding invite → <c>Provisioned</c> (0). Default already applied by the column's <c>DEFAULT 0</c> — the explicit UPDATE is a no-op for these rows but the rule is documented here for completeness.</description></item>
    /// </list>
    ///
    /// <para>
    /// Operator safety:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Backfill is idempotent. Re-running the migration UP-then-DOWN-then-UP yields the same end state.</description></item>
    ///   <item><description>The legacy <c>IsActive</c> column is intentionally NOT dropped. Phase 2A keeps it as the rollback path. Phase 2B drops it once all read-side queries are migrated.</description></item>
    ///   <item><description>The cross-schema lookup against <c>auth.Otps</c> assumes the Auth module's CreateModel migration has been applied. In this codebase it is — both migrations live in the same deployment unit. If your environment provisions schemas independently, gate this UPDATE with an existence check (already inlined via <c>OBJECT_ID</c>).</description></item>
    /// </list>
    ///
    /// <para>
    /// Uncertainties explicitly called out:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>We cannot tell from the data alone whether an <c>IsActive = 0</c> user without an outstanding invite is a never-onboarded provision OR an account that was previously active and later soft-disabled by some legacy path. We default to <c>Provisioned</c> because the latter case has no infrastructure today (no <c>Suspend</c> command, no admin lifecycle UI, and <c>DeactivateUserCommandHandler</c> has only just been wired to the new <c>Suspend</c> verb in this same phase). If your production data contains historically-deactivated rows, run the post-deploy correction script documented in the Phase 2A migration notes.</description></item>
    /// </list>
    /// </summary>
    public partial class AddAccountLifecycleState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the column with default Provisioned (0). The default is
            //    applied to existing rows; the UPDATEs below promote eligible
            //    rows to higher states.
            migrationBuilder.AddColumn<int>(
                name: "LifecycleState",
                schema: "security",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LifecycleState",
                schema: "security",
                table: "Users",
                column: "LifecycleState");

            // 2. Backfill — order matters. Each subsequent rule only touches
            //    rows that the previous rules left at the default (0).

            // Rule A: IsActive = 1 → Active (2).
            migrationBuilder.Sql(@"
UPDATE [security].[Users]
SET    [LifecycleState] = 2
WHERE  [IsActive] = 1
  AND  [LifecycleState] = 0;");

            // Rule B: IsActive = 0 AND primary email verified → Active (2).
            //         (Data anomaly recovery — see migration XML doc.)
            migrationBuilder.Sql(@"
UPDATE u
SET    u.[LifecycleState] = 2
FROM   [security].[Users] u
WHERE  u.[IsActive] = 0
  AND  u.[LifecycleState] = 0
  AND  EXISTS (
        SELECT 1
        FROM   [security].[Emails] e
        WHERE  e.[UserId]    = u.[Id]
          AND  e.[IsPrimary] = 1
          AND  e.[IsVerified] = 1);");

            // Rule C: external-only login users — Active (2).
            migrationBuilder.Sql(@"
UPDATE [security].[Users]
SET    [LifecycleState] = 2
WHERE  [IsActive] = 0
  AND  [LifecycleState] = 0
  AND  [PasswordHash] LIKE 'EXTERNAL-ONLY:%';");

            // Rule D: outstanding UserInvite OTP exists → PendingActivation (1).
            //         Cross-schema lookup; gated on auth.Otps existence so the
            //         migration is safe to run even if the Auth schema has
            //         been provisioned out of order in some non-standard env.
            migrationBuilder.Sql(@"
IF OBJECT_ID('auth.Otps', 'U') IS NOT NULL
BEGIN
    UPDATE u
    SET    u.[LifecycleState] = 1
    FROM   [security].[Users] u
    WHERE  u.[IsActive] = 0
      AND  u.[LifecycleState] = 0
      AND  EXISTS (
            SELECT 1
            FROM   [auth].[Otps] o
            WHERE  o.[UserId]  = u.[Id]
              AND  o.[Purpose] = 'UserInvite'
              AND  o.[IsUsed]  = 0
              AND  o.[ExpiresAt] > SYSUTCDATETIME());
END;");

            // Rule E (implicit): everything else stays at 0 = Provisioned.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_LifecycleState",
                schema: "security",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LifecycleState",
                schema: "security",
                table: "Users");
        }
    }
}
