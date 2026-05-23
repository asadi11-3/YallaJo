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