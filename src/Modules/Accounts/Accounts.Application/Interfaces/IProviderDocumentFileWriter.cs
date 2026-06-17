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
/// This is deliberately separate from <see cref="IProviderDocumentBackfillStore"/>: backfill is a
/// one-off maintenance concern, whereas this is a per-request runtime concern. Keeping it in
/// Accounts.Application keeps the handlers free of Infrastructure / EF Core / SqlClient references.
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
