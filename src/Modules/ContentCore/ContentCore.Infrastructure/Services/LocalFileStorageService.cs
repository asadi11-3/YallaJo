using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;

using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Local file system storage implementation.
/// Stores files under a configurable base path (default: wwwroot/uploads).
/// Swap for CloudinaryFileStorageService (or any other provider) later via DI.
/// </summary>
internal sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;

        // Configurable via appsettings: "FileStorage:BasePath" and "FileStorage:BaseUrl"
        _basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        _baseUrl = configuration["FileStorage:BaseUrl"]?.TrimEnd('/')
            ?? "/uploads";
    }

    public async Task<Result<FileUploadResult>> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        if (stream is null || stream.Length == 0)
            return Result<FileUploadResult>.Failure(
                new Error("FileStorage.EmptyStream", "File stream is empty."),
                Outcome.Invalid);

        if (string.IsNullOrWhiteSpace(fileName))
            return Result<FileUploadResult>.Failure(
                new Error("FileStorage.MissingFileName", "File name is required."),
                Outcome.Invalid);

        // Generate a unique file name to prevent collisions
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.CreateVersion7()}{extension}";
        var folderPath = Path.Combine(_basePath, folder);

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, uniqueName);

        long fileSize;
        await using (var fileStream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await stream.CopyToAsync(fileStream, ct);
            // CRITICAL: flush the FileStream's 81920-byte buffer BEFORE we
            // sample the on-disk size. Otherwise FileInfo.Length samples a
            // not-yet-flushed file and we persist FileSize=0 on the DB row
            // even though the file itself reaches disk correctly once the
            // stream is disposed at method exit. Reproduced on staging
            // proxy as part of the Blocker #2 verification.
            await fileStream.FlushAsync(ct);
            fileSize = fileStream.Length;
        }

        var storageKey = $"{folder}/{uniqueName}";
        var url = $"{_baseUrl}/{folder}/{uniqueName}";

        _logger.LogDebug(
            "File uploaded: {StorageKey} ({FileSize} bytes)",
            storageKey, fileSize);

        return Result<FileUploadResult>.Success(new FileUploadResult(url, storageKey, fileSize));
    }

    public Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return Task.FromResult(false);

        // Resolve the stored URL to a physical path, guarding against traversal.
        if (!TryResolvePhysicalPath(fileUrl, out var fullPath))
        {
            _logger.LogWarning("Rejected delete for URL resolving outside storage root.");
            return Task.FromResult(false);
        }

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found for deletion: {FilePath}", fullPath);
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        _logger.LogDebug("File deleted: {FilePath}", fullPath);
        return Task.FromResult(true);
    }

    public Task<string> GetAccessUrlAsync(string fileUrl, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        // Local storage is always public — return the URL as-is
        return Task.FromResult(fileUrl);
    }

    public Task<Result<FileDownload>> OpenReadAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return Task.FromResult(Result<FileDownload>.Failure(
                new Error("FileStorage.MissingUrl", "File URL is required."),
                Outcome.Invalid));

        // Resolve the stored URL to a physical path INSIDE the storage root.
        // Any URL that canonicalizes outside the root (path traversal) is rejected
        // as Invalid — we never reveal whether such a path exists.
        if (!TryResolvePhysicalPath(fileUrl, out var fullPath))
        {
            _logger.LogWarning("Rejected read for URL resolving outside storage root.");
            return Task.FromResult(Result<FileDownload>.Failure(
                new Error("FileStorage.InvalidPath", "The requested file path is invalid."),
                Outcome.Invalid));
        }

        return Task.FromResult(OpenResolvedPath(fullPath));
    }

    /// <summary>
    /// Patch 2C — open a stored file by its provider-relative storage key
    /// (e.g. <c>provider-application-documents/{guid}.pdf</c>), without any
    /// public base-URL prefix. Used by the FileAsset V2 read path so callers
    /// never see the physical layout. Same path-traversal guard as
    /// <see cref="OpenReadAsync"/>.
    /// </summary>
    public Task<Result<FileDownload>> OpenReadByStorageKeyAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return Task.FromResult(Result<FileDownload>.Failure(
                new Error("FileStorage.MissingStorageKey", "Storage key is required."),
                Outcome.Invalid));

        if (!TryResolvePhysicalPathFromStorageKey(storageKey, out var fullPath))
        {
            _logger.LogWarning("Rejected read for storage key resolving outside storage root.");
            return Task.FromResult(Result<FileDownload>.Failure(
                new Error("FileStorage.InvalidPath", "The requested file path is invalid."),
                Outcome.Invalid));
        }

        return Task.FromResult(OpenResolvedPath(fullPath));
    }

    /// <summary>
    /// Shared core for both <see cref="OpenReadAsync"/> and
    /// <see cref="OpenReadByStorageKeyAsync"/>: at this point the caller's
    /// input has already been canonicalized to a physical path inside the
    /// storage root.
    /// </summary>
    private Result<FileDownload> OpenResolvedPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            // NotFound: the storage row points to a missing blob (e.g. cleaned up).
            return Result<FileDownload>.Failure(
                new Error("FileStorage.NotFound", "The requested file was not found."),
                Outcome.NotFound);
        }

        FileStream stream;
        long length;
        try
        {
            var info = new FileInfo(fullPath);
            length = info.Length;
            // Caller OWNS and disposes this stream. Read-share so concurrent reads work.
            stream = new FileStream(
                fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to open stored file for reading.");
            return Result<FileDownload>.Failure(
                new Error("FileStorage.ReadFailed", "The requested file could not be read."),
                Outcome.NotFound);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied opening stored file for reading.");
            return Result<FileDownload>.Failure(
                new Error("FileStorage.ReadFailed", "The requested file could not be read."),
                Outcome.NotFound);
        }

        var contentType = ResolveContentType(fullPath);
        return Result<FileDownload>.Success(new FileDownload(stream, contentType, length));
    }

    /// <summary>
    /// Converts a stored relative web URL (e.g. <c>/uploads/folder/file.ext</c>) into a
    /// physical path and verifies the result stays inside the configured storage root.
    /// Returns <c>false</c> when the path canonicalizes outside the root (traversal attempt).
    /// Never throws and never exposes the resolved path to callers.
    /// </summary>
    private bool TryResolvePhysicalPath(string fileUrl, out string fullPath)
    {
        fullPath = string.Empty;

        var relativePath = fileUrl
            .Replace(_baseUrl, string.Empty)
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        // Reject obvious traversal tokens early; the canonical-root check below is authoritative.
        if (relativePath.Contains("..", StringComparison.Ordinal))
            return false;

        var candidate = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        var root = Path.GetFullPath(_basePath);

        // Ensure the root comparison includes a trailing separator so that
        // "/uploads-evil" cannot masquerade as being under "/uploads".
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return false;

        fullPath = candidate;
        return true;
    }

    /// <summary>
    /// Patch 2C — converts a provider-relative storage key (e.g. <c>folder/{guid}.ext</c>)
    /// directly into a physical path under the configured storage root. The key does
    /// NOT include any public base-URL prefix. Same canonicalization +
    /// outside-the-root rejection as <see cref="TryResolvePhysicalPath"/>.
    /// </summary>
    private bool TryResolvePhysicalPathFromStorageKey(string storageKey, out string fullPath)
    {
        fullPath = string.Empty;

        // Treat the key as already provider-relative: no _baseUrl strip step.
        var relativePath = storageKey
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        if (relativePath.Length == 0)
            return false;

        if (relativePath.Contains("..", StringComparison.Ordinal))
            return false;

        var candidate = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        var root = Path.GetFullPath(_basePath);

        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return false;

        fullPath = candidate;
        return true;
    }

    /// <summary>
    /// Best-effort MIME inference from extension. Defaults to a safe generic type.
    /// No external package dependency.
    /// </summary>
    private static string ResolveContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".tif" or ".tiff" => "image/tiff",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream",
        };
    }
}