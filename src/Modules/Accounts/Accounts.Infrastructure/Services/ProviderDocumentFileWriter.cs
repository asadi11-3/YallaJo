using Accounts.Application.Interfaces;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Services;

/// <summary>
/// Patch 2D runtime writer for the <c>accounts.ProviderDocumentFiles</c> link table.
/// <para>
/// Upserts the single current FileAsset link for a provider document. Because
/// <c>UX_ProviderDocumentFiles_ProviderDocumentId</c> is unique, an existing link is
/// updated in place (repointed to the new FileAsset) rather than inserting a second row.
/// A concurrent-insert race is reconciled into an update.
/// </para>
/// </summary>
public sealed class ProviderDocumentFileWriter(AccountsDbContext accountsDbContext)
    : IProviderDocumentFileWriter
{
    public async Task UpsertLinkAsync(
        Guid providerDocumentId,
        Guid fileAssetId,
        DocumentType documentType,
        CancellationToken ct)
    {
        var existing = await accountsDbContext.ProviderDocumentFiles
            .FirstOrDefaultAsync(f => f.ProviderDocumentId == providerDocumentId, ct);

        if (existing is not null)
        {
            existing.UpdateFileAsset(fileAssetId);
            await accountsDbContext.SaveChangesAsync(ct);
            return;
        }

        var link = ProviderDocumentFile.Create(providerDocumentId, fileAssetId, documentType);
        accountsDbContext.ProviderDocumentFiles.Add(link);

        try
        {
            await accountsDbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent request inserted the link first. Detach our failed insert and
            // repoint the winning row to the current FileAsset (one-doc-to-one-current-asset).
            accountsDbContext.Entry(link).State = EntityState.Detached;

            var raced = await accountsDbContext.ProviderDocumentFiles
                .FirstOrDefaultAsync(f => f.ProviderDocumentId == providerDocumentId, ct);

            if (raced is not null)
            {
                raced.UpdateFileAsset(fileAssetId);
                await accountsDbContext.SaveChangesAsync(ct);
            }
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
        && sql.Errors.Cast<SqlError>().Any(e => e.Number is 2627 or 2601);
}
