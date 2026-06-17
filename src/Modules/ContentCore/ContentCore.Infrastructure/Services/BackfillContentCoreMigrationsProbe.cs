using ContentCore.Contracts.Storage;
using ContentCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IBackfillContentCoreMigrationsProbe"/>. Encapsulates
/// the ContentCoreDbContext applied-migrations probe used by cross-module
/// backfill flows (e.g. Patch 2B provider documents).
/// </summary>
internal sealed class BackfillContentCoreMigrationsProbe(ContentCoreDbContext dbContext)
    : IBackfillContentCoreMigrationsProbe
{
    private const string FileAssetsMigrationId = "20260617125053_AddFileAssets";

    public async Task<bool> IsFileAssetsMigrationAppliedAsync(CancellationToken ct)
    {
        var applied = await dbContext.Database.GetAppliedMigrationsAsync(ct);
        return applied.Contains(FileAssetsMigrationId);
    }
}
