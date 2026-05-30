using ContentCore.Domain.Enums;

namespace ContentCore.Application.Interfaces;

/// <summary>
/// In-memory queue for background media processing jobs.
/// Jobs are enqueued after file upload and processed asynchronously by the
/// <see cref="MediaProcessingBackgroundService"/>.
/// </summary>
public interface IMediaProcessingQueue
{
    ValueTask EnqueueAsync(MediaProcessingJob job, CancellationToken ct = default);
}

public sealed record MediaProcessingJob(
    Guid AttachmentId,
    string FileUrl,
    AttachmentType AttachmentType);
