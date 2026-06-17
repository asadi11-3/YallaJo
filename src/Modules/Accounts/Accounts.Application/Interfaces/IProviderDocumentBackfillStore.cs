namespace Accounts.Application.Interfaces;

/// <summary>
/// Application-layer port encapsulating all infrastructure-side operations the
/// Patch 2B provider-document backfill handler needs:
/// <list type="bullet">
///   <item>Pre-flight check that the Patch 2A migrations are applied on BOTH module schemas.</item>
///   <item>Resolution of the public <c>FileStorage:BaseUrl</c> setting.</item>
///   <item>Existence check + insertion of a <c>ProviderDocumentFile</c> link row.</item>
/// </list>
/// Keeps Accounts.Application free of Accounts.Infrastructure, ContentCore.Infrastructure,
/// Microsoft.Data.SqlClient, and Microsoft.Extensions.Configuration references.
/// </summary>
public interface IProviderDocumentBackfillStore
{
    /// <summary>
    /// True only when BOTH Patch 2A migrations are applied:
    /// <c>20260617125129_AddProviderDocumentFiles</c> in <c>accounts</c> AND
    /// <c>20260617125053_AddFileAssets</c> in <c>content_core</c>. The handler refuses
    /// to run otherwise.
    /// </summary>
    Task<BackfillMigrationsStatus> CheckMigrationsAsync(CancellationToken ct);

    /// <summary>Reads <c>FileStorage:BaseUrl</c> (trim-end-slash); falls back to <c>/uploads</c>.</summary>
    string GetFileStorageBaseUrl();

    /// <summary>
    /// True when a <c>ProviderDocumentFile</c> row already exists for the given
    /// document. Used as a fast pre-skip check before any work begins.
    /// </summary>
    Task<bool> IsAlreadyLinkedAsync(Guid providerDocumentId, CancellationToken ct);

    /// <summary>
    /// Inserts and persists a new <c>ProviderDocumentFile</c> link row.
    /// The infrastructure implementation catches SQL Server unique-violations
    /// (2627/2601) on <c>UX_ProviderDocumentFiles_ProviderDocumentId</c>, detaches the
    /// failed entity, and surfaces the race via <see cref="LinkInsertResult.AlreadyLinked"/>.
    /// </summary>
    Task<LinkInsertResult> InsertLinkAsync(
        Guid providerDocumentId,
        Guid fileAssetId,
        Accounts.Domain.Enums.DocumentType documentType,
        CancellationToken ct);
}

/// <summary>Result of the pre-flight migration applied-check.</summary>
public sealed record BackfillMigrationsStatus(
    bool AccountsApplied,
    bool ContentCoreApplied)
{
    public bool BothApplied => AccountsApplied && ContentCoreApplied;
}

/// <summary>Outcome of <see cref="IProviderDocumentBackfillStore.InsertLinkAsync"/>.</summary>
public enum LinkInsertResult
{
    Inserted = 0,
    AlreadyLinked = 1,
}
