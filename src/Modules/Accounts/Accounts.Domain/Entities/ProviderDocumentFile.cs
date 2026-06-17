using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;

/// <summary>
/// Patch 2A (FileAsset V2 expand phase): links a provider <see cref="ProviderDocument"/>
/// to its current physical file in the ContentCore FileAssets table.
/// One provider document maps to exactly one current FileAsset (enforced by a unique
/// index on <see cref="ProviderDocumentId"/>).
///
/// <para>
/// <see cref="FileAssetId"/> is a plain <see cref="Guid"/> reference into
/// content_core.FileAssets. There is intentionally NO cross-module database foreign key
/// (module boundary): referential integrity to FileAssets is enforced in the application layer.
/// </para>
/// </summary>
public sealed class ProviderDocumentFile : BaseEntity
{
    private ProviderDocumentFile() { } // EF Core

    /// <summary>The owning provider document (accounts.ProviderDocuments.Id).</summary>
    public Guid ProviderDocumentId { get; private set; }

    /// <summary>The current physical file (content_core.FileAssets.Id). No DB FK by design.</summary>
    public Guid FileAssetId { get; private set; }

    /// <summary>Denormalized document type, mirrored from the owning provider document.</summary>
    public DocumentType DocumentType { get; private set; }

    public static ProviderDocumentFile Create(
        Guid providerDocumentId,
        Guid fileAssetId,
        DocumentType documentType)
    {
        if (providerDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Provider document id is required.", nameof(providerDocumentId));
        }

        if (fileAssetId == Guid.Empty)
        {
            throw new ArgumentException("File asset id is required.", nameof(fileAssetId));
        }

        return new ProviderDocumentFile
        {
            ProviderDocumentId = providerDocumentId,
            FileAssetId        = fileAssetId,
            DocumentType       = documentType,
        };
    }

    /// <summary>
    /// Patch 2D: repoints this link to a new current FileAsset (used by the replace-upload
    /// write path). One provider document always maps to exactly one current FileAsset, so this
    /// updates the existing link in place rather than inserting a second row.
    /// </summary>
    public void UpdateFileAsset(Guid newFileAssetId)
    {
        if (newFileAssetId == Guid.Empty)
        {
            throw new ArgumentException("File asset id is required.", nameof(newFileAssetId));
        }

        FileAssetId = newFileAssetId;
    }
}
