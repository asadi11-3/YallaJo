using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.RecordInteraction;

public sealed class RecordInteractionCommandHandler(IInteractionIngestQueue queue, HybridCache cache, ILogger<RecordInteractionCommandHandler> logger) : ICommandHandler<RecordInteractionCommand>
{
    public async Task<Result> Handle(RecordInteractionCommand request, CancellationToken ct)
    {
        var entityType = Enum.Parse<EntityType>(request.EntityType, true);
        var interactionType = Enum.Parse<InteractionType>(request.InteractionType, true);

        if (request.UserId is not null)
        {
            var dedupeKey = $"interaction-dedupe:{request.UserId.Value:N}:{entityType}:{request.EntityId:N}:{interactionType}";
            var sentinel = Guid.NewGuid().ToString("N");
            var observed = await cache.GetOrCreateAsync(
                dedupeKey,
                factory: _ => ValueTask.FromResult(sentinel),
                options: new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5),
                },
                tags: null,
                cancellationToken: ct).ConfigureAwait(false);

            if (!string.Equals(observed, sentinel, StringComparison.Ordinal))
            {
                logger.LogDebug("Skipped duplicate analytics interaction {EntityType}/{EntityId}", entityType, request.EntityId);
                return Result.Success();
            }
        }

        _ = queue.TryEnqueue(new InteractionEnvelope(request.UserId, request.SessionId, entityType, request.EntityId, interactionType, DateTime.UtcNow, request.ClientIp, request.UserAgent));
        logger.LogDebug("Queued analytics interaction {EntityType}/{EntityId}", entityType, request.EntityId);
        return Result.Success();
    }
}
