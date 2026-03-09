namespace ContentCore.Application.Interfaces;

/// <summary>
/// Extracts metadata from video and audio files, and generates video poster frames.
/// Current implementation uses FFMpegCore (wraps ffmpeg/ffprobe CLI).
/// Will be replaced by Cloudinary when migrating to cloud storage.
/// </summary>
public interface IVideoProcessingService
{
    /// <summary>
    /// Extracts duration, width, and height from a video file.
    /// </summary>
    Task<VideoProcessingResult?> ExtractVideoMetadataAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Extracts duration from an audio file.
    /// </summary>
    Task<AudioProcessingResult?> ExtractAudioMetadataAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Generates a poster-frame thumbnail from a video at ~5 seconds in.
    /// </summary>
    /// <returns>Relative URL of the generated thumbnail, or null on failure.</returns>
    Task<string?> GenerateVideoThumbnailAsync(string filePath, CancellationToken ct = default);
}

public sealed record VideoProcessingResult(
    int DurationSeconds,
    int Width,
    int Height);

public sealed record AudioProcessingResult(
    int DurationSeconds);
