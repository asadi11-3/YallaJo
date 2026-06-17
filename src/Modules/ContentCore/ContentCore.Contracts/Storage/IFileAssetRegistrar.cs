using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Contracts.Storage;

/// <summary>
/// Cross-module port that lets other modules (currently <c>Accounts</c>, for
/// the Patch 2B provider-document backfill) materialize a <c>FileAsset</c> row
/// without taking a dependency on <c>ContentCoreDbContext</c>.
/// <para>
/// This contract is intentionally narrow: it only supports the
/// <em>get-or-add by storage key</em> idempotent insertion path required by
/// the backfill. The <c>StorageKey</c> must remain INTERNAL — it is the
/// physical storage coordinate and MUST NEVER be exposed to API clients.
/// Callers in other modules treat the returned <see cref="FileAssetRecord.Id"/>
/// as an opaque link to a content_core.FileAssets row.
/// </para>
/// </summary>
public interface IFileAssetRegistrar
{
    /// <summary>
    /// Returns the existing <c>FileAsset</c> matching <paramref name="seed"/>'s
    /// <c>StorageKey</c> (uniquely indexed via <c>UX_FileAssets_StorageKey</c>),
    /// or creates a new one when none exists.
    /// </summary>
    /// <param name="seed">Physical metadata used to populate a new FileAsset
    /// when no row matches the storage key.</param>
    /// <param name="dryRun">When <c>true</c>, no row is inserted and the
    /// returned <see cref="FileAssetRecord.Id"/> is <c>Guid.Empty</c> for the
    /// would-be-inserted path. When an existing row is found, the real id is
    /// always returned regardless of <paramref name="dryRun"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Idempotency: implementations MUST be safe to call concurrently or repeatedly
    /// for the same <c>StorageKey</c>. When two callers race the insert, the
    /// later caller MUST observe a unique-violation (SQL 2627/2601) and return
    /// the winning row with <see cref="FileAssetRecord.WasReused"/> = <c>true</c>.
    /// </remarks>
    Task<Result<FileAssetRecord>> GetOrAddByStorageKeyAsync(
        FileAssetSeed seed,
        bool dryRun,
        CancellationToken ct = default);
}

/// <summary>
/// Inputs needed to create a new <c>FileAsset</c>. All fields mirror the
/// physical metadata stored on content_core.FileAssets.
/// </summary>
/// <param name="StorageProvider">Provider key (e.g. <c>"Local"</c>).</param>
/// <param name="StorageKey">Provider-relative key — INTERNAL, never exposed to API clients.</param>
/// <param name="OriginalFileName">User-supplied original file name (may contain unsafe chars).</param>
/// <param name="SafeFileName">Sanitized download-safe name (no path/separator chars).</param>
/// <param name="ContentType">MIME content-type (e.g. <c>application/pdf</c>).</param>
/// <param name="Extension">Lower-cased extension including the dot (e.g. <c>.pdf</c>).</param>
/// <param name="SizeBytes">File size in bytes (must be &gt;= 0).</param>
/// <param name="UploadedByUserId">User id stored as the uploader. For legacy
/// backfill rows this is the owning ProviderApplication.UserId (best
/// available proxy — original uploader identity was not retained).</param>
public sealed record FileAssetSeed(
    string StorageProvider,
    string StorageKey,
    string OriginalFileName,
    string SafeFileName,
    string ContentType,
    string Extension,
    long SizeBytes,
    Guid UploadedByUserId);

/// <summary>
/// Outcome of <see cref="IFileAssetRegistrar.GetOrAddByStorageKeyAsync"/>.
/// </summary>
/// <param name="Id">The FileAsset id. Will be <c>Guid.Empty</c> when
/// <c>dryRun</c> is true AND no existing row was found (the would-be-inserted
/// case).</param>
/// <param name="WasReused"><c>true</c> when an existing row matching the
/// storage key was returned; <c>false</c> when a new row was inserted (or
/// would be inserted in dry-run).</param>
public sealed record FileAssetRecord(Guid Id, bool WasReused);
