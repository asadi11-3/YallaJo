using Accounts.Application.Interfaces;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Infrastructure.Persistence;
using ContentCore.Contracts.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Accounts.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IProviderDocumentBackfillStore"/>.
/// Keeps all Accounts-side EF + SqlClient + IConfiguration concerns out of
/// Accounts.Application. Uses the per-module <c>accounts</c>-schema migration
/// history table for the pre-flight check; the ContentCore side queries are
/// done via an injected secondary store so we never reference
/// <c>ContentCore.Infrastructure</c> directly from Accounts.Infrastructure.
/// </summary>
public sealed class ProviderDocumentBackfillStore(
    AccountsDbContext accountsDbContext,
    IBackfillContentCoreMigrationsProbe contentCoreProbe,
    IConfiguration configuration)
    : IProviderDocumentBackfillStore
{
    private const string AccountsMigrationId = "20260617125129_AddProviderDocumentFiles";

    public async Task<BackfillMigrationsStatus> CheckMigrationsAsync(CancellationToken ct)
    {
        var accountsApplied = await accountsDbContext.Database
            .GetAppliedMigrationsAsync(ct);
        var accountsOk = accountsApplied.Contains(AccountsMigrationId);

        var contentCoreOk = await contentCoreProbe.IsFileAssetsMigrationAppliedAsync(ct);

        return new BackfillMigrationsStatus(accountsOk, contentCoreOk);
    }

    public string GetFileStorageBaseUrl()
        => (configuration["FileStorage:BaseUrl"] ?? "/uploads").TrimEnd('/');

    public Task<bool> IsAlreadyLinkedAsync(Guid providerDocumentId, CancellationToken ct)
        => accountsDbContext.ProviderDocumentFiles
            .AsNoTracking()
            .AnyAsync(f => f.ProviderDocumentId == providerDocumentId, ct);

    public async Task<LinkInsertResult> InsertLinkAsync(
        Guid providerDocumentId,
        Guid fileAssetId,
        DocumentType documentType,
        CancellationToken ct)
    {
        var link = ProviderDocumentFile.Create(providerDocumentId, fileAssetId, documentType);
        accountsDbContext.ProviderDocumentFiles.Add(link);

        try
        {
            await accountsDbContext.SaveChangesAsync(ct);
            return LinkInsertResult.Inserted;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            accountsDbContext.Entry(link).State = EntityState.Detached;
            return LinkInsertResult.AlreadyLinked;
        }
    }

    /// <summary>
    /// SQL Server unique-constraint (2627) / unique-index (2601) violation classifier.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sql)
        {
            foreach (SqlError error in sql.Errors)
            {
                if (error.Number == 2627 || error.Number == 2601)
                    return true;
            }
        }
        return false;
    }
}


