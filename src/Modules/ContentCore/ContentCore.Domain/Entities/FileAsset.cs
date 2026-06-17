using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

/// <summary>
/// V2 physical file metadata model (Patch 2A).
/// FileAsset describes ONLY the stored physical file (provider, key, size, hash, dimensions).
/// It deliberately knows NOTHING about which business entity owns it — business ownership lives
/// in module-specific ownership tables (e.g. accounts.ProviderDocumentFiles) that reference this
/// asset by a plain <c>FileAssetId</c> with no cross-module database FK.
/// <para>
/// SECURITY: <see cref="StorageKey"/> is an internal storage locator and must NEVER be exposed to
/// API clients. Downloads are mediated through authorized endpoints that stream bytes.
/// </para>
/// </summary>
public sealed class FileAsset : AuditableEntity, IAggregateRoot
{
    private FileAsset() { } // EF Core

    /// <summary>Logical storage backend, e.g. "Local" (today) or "S3"/"AzureBlob" (future).</summary>
    public string StorageProvider { get; private set; } = string.Empty;

    /// <summary>Internal storage locator (relative key/path). NEVER exposed to API clients.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    /// <summary>Original client-supplied file name (for display / download naming).</summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>Sanitized file name safe for storage / Content-Disposition.</summary>
    public string SafeFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    /// <summary>File extension including the leading dot, lower-cased (e.g. ".pdf").</summary>
    public string Extension { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    /// <summary>Lower-case hex SHA-256 of the file content, when computed. Optional in 2A.</summary>
    public string? Sha256 { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public int? DurationSeconds { get; private set; }

    /// <summary>
    /// The ONLY way to create a FileAsset. No domain event is raised in Patch 2A (additive schema only).
    /// </summary>
    public static FileAsset Create(
        string storageProvider,
        string storageKey,
        string originalFileName,
        string safeFileName,
        string contentType,
        string extension,
        long sizeBytes,
        Guid uploadedByUserId,
        string? sha256 = null,
        int? width = null,
        int? height = null,
        int? durationSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(storageProvider))
            throw new ArgumentException("Storage provider is required.", nameof(storageProvider));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        if (sizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size cannot be negative.");

        return new FileAsset
        {
            StorageProvider = storageProvider.Trim(),
            StorageKey = storageKey.Trim(),
            OriginalFileName = (originalFileName ?? string.Empty).Trim(),
            SafeFileName = (safeFileName ?? string.Empty).Trim(),
            ContentType = contentType.Trim(),
            Extension = (extension ?? string.Empty).Trim().ToLowerInvariant(),
            SizeBytes = sizeBytes,
            Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256.Trim().ToLowerInvariant(),
            UploadedByUserId = uploadedByUserId,
            Width = width,
            Height = height,
            DurationSeconds = durationSeconds,
        };
    }

    public void SetDimensions(int? width, int? height)
    {
        Width = width;
        Height = height;
    }

    public void SetDuration(int? durationSeconds)
    {
        DurationSeconds = durationSeconds;
    }

    public void SetHash(string sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256))
            throw new ArgumentException("Hash is required.", nameof(sha256));

        Sha256 = sha256.Trim().ToLowerInvariant();
    }
}
