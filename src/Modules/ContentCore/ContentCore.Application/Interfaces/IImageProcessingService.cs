namespace ContentCore.Application.Interfaces;

/// <summary>
/// Generates thumbnails and extracts dimensions from image files.
/// Current implementation: SixLabors.ImageSharp.
/// Future: will be replaced by Cloudinary SDK when migrating to cloud storage.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Processes an image file: extracts original dimensions and generates a thumbnail.
    /// </summary>
    /// <param name="filePath">Absolute path to the image file on disk.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Processing result with dimensions and thumbnail path, or null if processing fails.</returns>
    Task<ImageProcessingResult?> ProcessAsync(string filePath, CancellationToken ct = default);
}

public sealed record ImageProcessingResult(
    int OriginalWidth,
    int OriginalHeight,
    string? ThumbnailRelativeUrl);
