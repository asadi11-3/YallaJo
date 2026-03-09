using ContentCore.Application.Interfaces;
using FFMpegCore;
using FFMpegCore.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Extracts metadata from video/audio files and generates video poster frames
/// using FFMpegCore (wraps ffmpeg/ffprobe CLI).
/// Will be replaced by Cloudinary when migrating to cloud storage.
///
/// Requires ffmpeg + ffprobe binaries on PATH or configured via
/// "FFmpeg:BinaryFolder" in appsettings.json.
/// </summary>
internal sealed class VideoProcessingService : IVideoProcessingService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(
        IConfiguration configuration,
        ILogger<VideoProcessingService> logger)
    {
        _logger = logger;

        _basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        _baseUrl = configuration["FileStorage:BaseUrl"]?.TrimEnd('/')
            ?? "/uploads";

        // Configure ffmpeg binary path if specified
        var ffmpegPath = configuration["FFmpeg:BinaryFolder"];
        if (!string.IsNullOrWhiteSpace(ffmpegPath))
        {
            GlobalFFOptions.Configure(new FFOptions
            {
                BinaryFolder = ffmpegPath,
                TemporaryFilesFolder = Path.GetTempPath()
            });
        }
    }

    public async Task<VideoProcessingResult?> ExtractVideoMetadataAsync(
        string filePath, CancellationToken ct = default)
    {
        var media = await SafeAnalyseAsync(filePath, ct);
        if (media?.PrimaryVideoStream is not { } video)
        {
            _logger.LogWarning("No video stream found in: {FilePath}", filePath);
            return null;
        }

        var result = new VideoProcessingResult(
            DurationSeconds: (int)media.Duration.TotalSeconds,
            Width: video.Width,
            Height: video.Height);

        _logger.LogInformation(
            "Video metadata extracted: {FilePath} — {Duration}s, {Width}×{Height}",
            filePath, result.DurationSeconds, result.Width, result.Height);

        return result;
    }

    public async Task<AudioProcessingResult?> ExtractAudioMetadataAsync(
        string filePath, CancellationToken ct = default)
    {
        var media = await SafeAnalyseAsync(filePath, ct);
        if (media?.PrimaryAudioStream is null)
        {
            _logger.LogWarning("No audio stream found in: {FilePath}", filePath);
            return null;
        }

        var result = new AudioProcessingResult(
            DurationSeconds: (int)media.Duration.TotalSeconds);

        _logger.LogInformation(
            "Audio metadata extracted: {FilePath} — {Duration}s",
            filePath, result.DurationSeconds);

        return result;
    }

    public async Task<string?> GenerateVideoThumbnailAsync(
        string filePath, CancellationToken ct = default)
    {
        try
        {
            var media = await SafeAnalyseAsync(filePath, ct);
            if (media?.PrimaryVideoStream is null)
                return null;

            // Capture at 5 seconds or 10% of duration, whichever is less
            var captureAt = TimeSpan.FromSeconds(
                Math.Min(5.0, media.Duration.TotalSeconds * 0.1));

            var directory = Path.GetDirectoryName(filePath)!;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            var thumbnailFileName = $"{nameWithoutExt}_poster.jpg";
            var thumbnailPath = Path.Combine(directory, thumbnailFileName);

            // Generate poster frame — width 640, height auto (preserves aspect ratio)
            await FFMpeg.SnapshotAsync(
                filePath,
                thumbnailPath,
                new System.Drawing.Size(640, -1),
                captureAt);

            if (!File.Exists(thumbnailPath))
            {
                _logger.LogWarning("Poster frame was not created: {ThumbnailPath}", thumbnailPath);
                return null;
            }

            // Build relative URL
            var relativePath = Path.GetRelativePath(_basePath, thumbnailPath)
                .Replace(Path.DirectorySeparatorChar, '/');
            var thumbnailUrl = $"{_baseUrl}/{relativePath}";

            _logger.LogInformation(
                "Video poster frame generated: {ThumbnailUrl}", thumbnailUrl);

            return thumbnailUrl;
        }
        catch (FFMpegException ex)
        {
            _logger.LogError(ex, "Video thumbnail generation failed: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error generating video thumbnail: {FilePath}", filePath);
            return null;
        }
    }

    private async Task<IMediaAnalysis?> SafeAnalyseAsync(string filePath, CancellationToken ct)
    {
        try
        {
            return await FFProbe.AnalyseAsync(filePath, cancellationToken: ct);
        }
        catch (FFMpegException ex) when (ex.Type == FFMpegExceptionType.File)
        {
            _logger.LogWarning("Media file not found: {FilePath}", filePath);
            return null;
        }
        catch (FFMpegException ex) when (ex.Type == FFMpegExceptionType.Process)
        {
            _logger.LogError(ex,
                "ffprobe process failed (binary missing or corrupt file): {FilePath}", filePath);
            return null;
        }
        catch (FormatNullException ex)
        {
            _logger.LogError(ex, "Could not parse media format: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error analysing media: {FilePath}", filePath);
            return null;
        }
    }
}
