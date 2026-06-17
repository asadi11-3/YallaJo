namespace ContentCore.Contracts.Storage;

/// <summary>
/// Cross-module probe used by other modules' backfill flows (Patch 2B) to
/// verify the ContentCore Patch 2A migration <c>AddFileAssets</c> is applied
/// on the <c>content_core</c> schema, without taking a project reference to
/// <c>ContentCore.Infrastructure</c>.
/// <para>Implemented in <c>ContentCore.Infrastructure</c>.</para>
/// </summary>
public interface IBackfillContentCoreMigrationsProbe
{
    /// <summary>True when migration <c>20260617125053_AddFileAssets</c> appears in the applied list.</summary>
    Task<bool> IsFileAssetsMigrationAppliedAsync(CancellationToken ct);
}
