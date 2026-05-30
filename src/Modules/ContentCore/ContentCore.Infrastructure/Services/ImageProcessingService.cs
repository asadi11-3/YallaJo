using ContentCore.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Generates thumbnails and extracts dimensions using SixLabors.ImageSharp.
/// Produces a single WebP thumbnail (~300×300, aspect-ratio preserved).
/// Will be replaced by Cloudinary when migrating to cloud storage.
/// </summary>
internal sealed class ImageProcessingService : IImageProcessingService
{
    private const int ThumbnailMaxDimension = 300;

    private static readonly WebpEncoder ThumbnailEncoder = new()
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = 80
    };

    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(
        IConfiguration configuration,
        ILogger<ImageProcessingService> logger)
    {
        _logger = logger;

        _basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        _baseUrl = configuration["FileStorage:BaseUrl"]?.TrimEnd('/')
            ?? "/uploads";
    }

    public async Task<ImageProcessingResult?> ProcessAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            using var image = await Image.LoadAsync(filePath, ct);

            // Auto-orient based on EXIF (critical for mobile uploads)
            image.Mutate(ctx => ctx.AutoOrient());

            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // Generate thumbnail — fit within 300×300 preserving aspect ratio
            string? thumbnailRelativeUrl = null;

            if (originalWidth > ThumbnailMaxDimension || originalHeight > ThumbnailMaxDimension)
            {
                using var thumbnail = image.Clone(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(ThumbnailMaxDimension, ThumbnailMaxDimension),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));

                var directory = Path.GetDirectoryName(filePath)!;
                var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var thumbnailFileName = $"{nameWithoutExt}_thumb.webp";
                var thumbnailPath = Path.Combine(directory, thumbnailFileName);

                await thumbnail.SaveAsWebpAsync(thumbnailPath, ThumbnailEncoder, ct);

                // Build relative URL from file path
                var relativePath = Path.GetRelativePath(_basePath, thumbnailPath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                thumbnailRelativeUrl = $"{_baseUrl}/{relativePath}";

                _logger.LogInformation(
                    "Generated thumbnail: {ThumbnailPath} ({Width}×{Height})",
                    thumbnailRelativeUrl, thumbnail.Width, thumbnail.Height);
            }
            else
            {
                _logger.LogDebug(
                    "Image {FilePath} is already small ({Width}×{Height}), skipping thumbnail generation",
                    filePath, originalWidth, originalHeight);
            }

            return new ImageProcessingResult(originalWidth, originalHeight, thumbnailRelativeUrl);
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogWarning(ex, "Unsupported image format: {FilePath}", filePath);
            return null;
        }
        catch (InvalidImageContentException ex)
        {
            _logger.LogWarning(ex, "Corrupt image file: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Image processing failed: {FilePath}", filePath);
            return null;
        }
    }
}
