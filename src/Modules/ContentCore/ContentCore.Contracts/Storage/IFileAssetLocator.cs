using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Contracts.Storage;

/// <summary>
/// Cross-module read port that lets other modules (currently <c>Accounts</c>,
/// for the Patch 2C provider-document download switch) resolve a FileAsset
/// row by id without taking a dependency on <c>ContentCoreDbContext</c>.
/// <para>
/// The returned <see cref="FileAssetView"/> carries the physical
/// <c>StorageKey</c>, which is INTERNAL — authorized handlers feed it back
/// into <see cref="YallaJo.SharedKernel.Application.Abstractions.Storage.IFileStorageService.OpenReadByStorageKeyAsync(string, System.Threading.CancellationToken)"/>.
/// It MUST NEVER be projected onto any client-visible DTO, response body,
/// log message, or telemetry payload.
/// </para>
/// </summary>
public interface IFileAssetLocator
{
    /// <summary>
    /// Returns the (non-deleted) FileAsset matching <paramref name="fileAssetId"/>,
    /// or a NotFound failure when no such row exists. The lookup honors the
    /// FileAsset soft-delete query filter.
    /// </summary>
    /// <param name="fileAssetId">FileAsset primary key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<FileAssetView>> GetByIdAsync(Guid fileAssetId, CancellationToken ct = default);
}

/// <summary>
/// Read-only projection of a FileAsset row. The <see cref="StorageKey"/>
/// MUST stay inside authorized server-side code paths.
/// </summary>
/// <param name="Id">FileAsset primary key.</param>
/// <param name="StorageProvider">Provider key (e.g. <c>"Local"</c>).</param>
/// <param name="StorageKey">Provider-relative key — INTERNAL, never exposed to API clients.</param>
/// <param name="ContentType">MIME content-type (e.g. <c>application/pdf</c>).</param>
/// <param name="Extension">Lower-cased extension including the dot (e.g. <c>.pdf</c>).</param>
/// <param name="OriginalFileName">User-supplied original file name (may contain unsafe chars).</param>
/// <param name="SafeFileName">Sanitized download-safe name (no path/separator chars).</param>
/// <param name="SizeBytes">File size in bytes.</param>
public sealed record FileAssetView(
    Guid Id,
    string StorageProvider,
    string StorageKey,
    string ContentType,
    string Extension,
    string OriginalFileName,
    string SafeFileName,
    long SizeBytes);
