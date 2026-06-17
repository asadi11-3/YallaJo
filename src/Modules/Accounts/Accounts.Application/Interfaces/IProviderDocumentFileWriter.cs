using Accounts.Domain.Enums;

namespace Accounts.Application.Interfaces;

/// <summary>
/// Patch 2D runtime write port for the provider-document FileAsset V2 link table
/// (<c>accounts.ProviderDocumentFiles</c>).
/// <para>
/// Used by the upload and replace-upload write paths to materialise the
/// <c>ProviderDocument -&gt; ProviderDocumentFile -&gt; FileAsset</c> link as soon as a document
/// is stored, so that the Patch 2C download read path immediately prefers the FileAsset source.
/// </para>
/// <para>
/// Keeping this port in Accounts.Application keeps the upload/replace handlers free of
/// Infrastructure / EF Core / SqlClient references. (The Patch 2B backfill store that was
/// the historical other implementer of cross-module link writes was removed in Patch 2G
/// when the legacy ProviderDocument file columns were dropped.)
/// </para>
/// </summary>
public interface IProviderDocumentFileWriter
{
    /// <summary>
    /// Creates the <c>ProviderDocumentFile</c> link for the document, or repoints the existing
    /// link to <paramref name="fileAssetId"/> when one already exists.
    /// <para>
    /// Because <c>UX_ProviderDocumentFiles_ProviderDocumentId</c> is unique, a provider document
    /// always maps to exactly one current FileAsset; this method never inserts a second row.
    /// Concurrent inserts that lose the unique-index race are reconciled into an update.
    /// </para>
    /// </summary>
    Task UpsertLinkAsync(
        Guid providerDocumentId,
        Guid fileAssetId,
        DocumentType documentType,
        CancellationToken ct);
}
