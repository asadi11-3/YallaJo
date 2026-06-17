using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace YallaJo.SharedKernel.Application.Abstractions.Storage;

/// <summary>
/// Abstraction for file storage operations.
/// Implementations may target local disk, Azure Blob Storage, Cloudinary, S3, etc.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file and return its publicly accessible URL.
    /// </summary>
    /// <param name="stream">File content stream.</param>
    /// <param name="fileName">Original file name (used for extension/content type inference).</param>
    /// <param name="contentType">MIME type (e.g. "image/jpeg").</param>
    /// <param name="folder">Logical folder/container (e.g. "places", "blogs", "tours").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored file's accessible URL.</returns>
    Task<Result<FileUploadResult>> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a file by its URL or storage key.
    /// </summary>
    /// <returns>True if the file was found and deleted; false otherwise.</returns>
    Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default);

    /// <summary>
    /// Open a readable stream for a previously stored file, identified by the same
    /// URL/storage key that <see cref="UploadAsync"/> returned.
    /// <para>
    /// This is intended for AUTHORIZED, server-mediated downloads: the caller (an
    /// application/handler that has already verified the requester's ownership or
    /// permission) streams the bytes back to the client WITHOUT exposing the
    /// physical path or storage key. Implementations MUST guard against path
    /// traversal and MUST NOT serve content outside their configured storage root.
    /// </para>
    /// </summary>
    /// <param name="fileUrl">The stored file URL/key (as returned by <see cref="UploadAsync"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful result carrying the open <see cref="FileDownload"/> (caller owns/disposes the stream),
    /// or a failure result (NotFound / Invalid) — never an exception for the expected "missing file" case.
    /// </returns>
    Task<Result<FileDownload>> OpenReadAsync(string fileUrl, CancellationToken ct = default);

    /// <summary>
    /// Generate a time-limited access URL for a file (useful for private storage providers like Cloudinary signed URLs).
    /// For public storage (e.g. local disk), this simply returns the original URL.
    /// </summary>
    Task<string> GetAccessUrlAsync(string fileUrl, TimeSpan? expiry = null, CancellationToken ct = default);
}

/// <summary>
/// Result of a file upload operation.
/// </summary>
public sealed record FileUploadResult(
    /// <summary>Publicly accessible URL of the uploaded file.</summary>
    string Url,
    /// <summary>Unique storage key/path (provider-specific, for deletion).</summary>
    string StorageKey,
    /// <summary>File size in bytes.</summary>
    long FileSize);

/// <summary>
/// An open, readable handle to a stored file for an authorized, server-mediated download.
/// The <see cref="Content"/> stream is owned by the caller and must be disposed.
/// No physical path or storage key is exposed.
/// </summary>
public sealed record FileDownload(
    /// <summary>Readable content stream (caller disposes).</summary>
    Stream Content,
    /// <summary>Best-known MIME type for the file (may be a generic fallback).</summary>
    string ContentType,
    /// <summary>File size in bytes, or null if not cheaply known.</summary>
    long? FileSize);