using ContentCore.Application.Interfaces;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentCore.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that reads from the <see cref="MediaProcessingQueue"/> channel
/// and dispatches jobs to the appropriate media processing service.
///
/// For each job it:
///   1. Resolves the file URL to a physical path
///   2. Dispatches to IImageProcessingService or IVideoProcessingService
///   3. Updates the Attachment entity with extracted metadata (dimensions, duration, thumbnail)
///   4. Persists changes via UnitOfWork
///
/// Uses a scoped DI container per job to respect EF Core DbContext lifetime.
/// </summary>
internal sealed class MediaProcessingBackgroundService(
    MediaProcessingQueue queue,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<MediaProcessingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(3);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("MediaProcessingBackgroundService started");

        // Short delay to let the app finish starting
        try { await Task.Delay(InitialDelay, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }

        await foreach (var job in queue.Reader.ReadAllAsync(ct))
        {
            try
            {
                await ProcessJobAsync(job, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Media processing failed for attachment {AttachmentId} ({Type})",
                    job.AttachmentId, job.AttachmentType);
            }
        }

        logger.LogInformation("MediaProcessingBackgroundService stopped");
    }

    private async Task ProcessJobAsync(MediaProcessingJob job, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing media: Attachment {AttachmentId}, Type {AttachmentType}",
            job.AttachmentId, job.AttachmentType);

        var filePath = ResolvePhysicalPath(job.FileUrl);
        if (filePath is null || !File.Exists(filePath))
        {
            logger.LogWarning(
                "Physical file not found for attachment {AttachmentId}: {FileUrl}",
                job.AttachmentId, job.FileUrl);
            return;
        }

        using var scope = serviceProvider.CreateScope();

        switch (job.AttachmentType)
        {
            case AttachmentType.Image:
                await ProcessImageAsync(scope, job, filePath, ct);
                break;

            case AttachmentType.Video:
                await ProcessVideoAsync(scope, job, filePath, ct);
                break;

            case AttachmentType.Audio:
                await ProcessAudioAsync(scope, job, filePath, ct);
                break;

            case AttachmentType.Document:
                logger.LogDebug("No media processing needed for documents");
                break;
        }
    }

    private async Task ProcessImageAsync(
        IServiceScope scope, MediaProcessingJob job, string filePath, CancellationToken ct)
    {
        var imageService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
        var result = await imageService.ProcessAsync(filePath, ct);
        if (result is null) return;

        var repo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IContentCoreUnitOfWork>();

        var attachment = await repo.GetByIdAsync(job.AttachmentId, ct);
        if (attachment is null)
        {
            logger.LogWarning("Attachment {AttachmentId} not found in database", job.AttachmentId);
            return;
        }

        attachment.SetDimensions(result.OriginalWidth, result.OriginalHeight);

        if (result.ThumbnailRelativeUrl is not null)
            attachment.SetThumbnailUrl(result.ThumbnailRelativeUrl);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Image processing complete: {AttachmentId} — {Width}×{Height}, Thumbnail: {HasThumb}",
            job.AttachmentId, result.OriginalWidth, result.OriginalHeight,
            result.ThumbnailRelativeUrl is not null);
    }

    private async Task ProcessVideoAsync(
        IServiceScope scope, MediaProcessingJob job, string filePath, CancellationToken ct)
    {
        var videoService = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();

        // Extract metadata
        var metadata = await videoService.ExtractVideoMetadataAsync(filePath, ct);

        // Generate poster frame
        var posterUrl = await videoService.GenerateVideoThumbnailAsync(filePath, ct);

        if (metadata is null && posterUrl is null) return;

        var repo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IContentCoreUnitOfWork>();

        var attachment = await repo.GetByIdAsync(job.AttachmentId, ct);
        if (attachment is null)
        {
            logger.LogWarning("Attachment {AttachmentId} not found in database", job.AttachmentId);
            return;
        }

        if (metadata is not null)
        {
            attachment.SetDimensions(metadata.Width, metadata.Height);
            attachment.SetDuration(metadata.DurationSeconds);
        }

        if (posterUrl is not null)
            attachment.SetThumbnailUrl(posterUrl);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Video processing complete: {AttachmentId} — {Duration}s, {Width}×{Height}, Poster: {HasPoster}",
            job.AttachmentId,
            metadata?.DurationSeconds ?? 0, metadata?.Width ?? 0, metadata?.Height ?? 0,
            posterUrl is not null);
    }

    private async Task ProcessAudioAsync(
        IServiceScope scope, MediaProcessingJob job, string filePath, CancellationToken ct)
    {
        var videoService = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();
        var metadata = await videoService.ExtractAudioMetadataAsync(filePath, ct);
        if (metadata is null) return;

        var repo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IContentCoreUnitOfWork>();

        var attachment = await repo.GetByIdAsync(job.AttachmentId, ct);
        if (attachment is null)
        {
            logger.LogWarning("Attachment {AttachmentId} not found in database", job.AttachmentId);
            return;
        }

        attachment.SetDuration(metadata.DurationSeconds);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Audio processing complete: {AttachmentId} — {Duration}s",
            job.AttachmentId, metadata.DurationSeconds);
    }

    /// <summary>
    /// Converts a relative URL (e.g. /uploads/places/abc.jpg) to an absolute file path.
    /// </summary>
    private string? ResolvePhysicalPath(string fileUrl)
    {
        var basePath = configuration["FileStorage:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        var baseUrl = configuration["FileStorage:BaseUrl"]?.TrimEnd('/') ?? "/uploads";

        if (!fileUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("File URL does not match base URL: {FileUrl}", fileUrl);
            return null;
        }

        var relativePath = fileUrl[baseUrl.Length..]
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.Combine(basePath, relativePath);
    }
}
