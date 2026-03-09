using System.Threading.Channels;
using ContentCore.Application.Interfaces;

namespace ContentCore.Infrastructure.BackgroundJobs;

/// <summary>
/// In-memory bounded channel for media processing jobs.
/// Registered as a Singleton — shared between producers (command handlers)
/// and the consumer (<see cref="MediaProcessingBackgroundService"/>).
///
/// Bounded to 100 items to apply back-pressure if the consumer falls behind.
/// Jobs are lost on app restart — acceptable for thumbnails/metadata
/// (the attachment still exists, just without generated assets).
/// </summary>
internal sealed class MediaProcessingQueue : IMediaProcessingQueue
{
    private readonly Channel<MediaProcessingJob> _channel = Channel.CreateBounded<MediaProcessingJob>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(MediaProcessingJob job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    internal ChannelReader<MediaProcessingJob> Reader => _channel.Reader;
}
