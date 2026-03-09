using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;

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

    public async Task<FileUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        if (stream is null || stream.Length == 0)
            throw new ArgumentException("File stream is empty.", nameof(stream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        // Generate a unique file name to prevent collisions
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.CreateVersion7()}{extension}";
        var folderPath = Path.Combine(_basePath, folder);

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, uniqueName);

        await using var fileStream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await stream.CopyToAsync(fileStream, ct);

        var fileSize = new FileInfo(filePath).Length;
        var storageKey = $"{folder}/{uniqueName}";
        var url = $"{_baseUrl}/{folder}/{uniqueName}";

        _logger.LogDebug(
            "File uploaded: {StorageKey} ({FileSize} bytes)",
            storageKey, fileSize);

        return new FileUploadResult(url, storageKey, fileSize);
    }

    public Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return Task.FromResult(false);

        // Convert URL back to file path
        var relativePath = fileUrl.Replace(_baseUrl, string.Empty).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, relativePath);

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
}