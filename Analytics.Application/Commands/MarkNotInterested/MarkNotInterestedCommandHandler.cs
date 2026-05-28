using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.MarkNotInterested;

public sealed class MarkNotInterestedCommandHandler(
    IInteractionIngestQueue queue,
    IUserExcludedEntityRepository excludedRepo,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<MarkNotInterestedCommandHandler> logger) : ICommandHandler<MarkNotInterestedCommand>
{
    public async Task<Result> Handle(MarkNotInterestedCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Check if already excluded
        var alreadyExcluded = await excludedRepo.IsExcludedAsync(request.UserId, request.EntityKind, request.EntityId, ct).ConfigureAwait(false);
        if (!alreadyExcluded)
        {
            // Insert exclusion row with 90-day expiry
            var exclusion = UserExcludedEntity.Create(request.UserId, request.EntityKind, request.EntityId, now.AddDays(90));
            await excludedRepo.AddAsync(exclusion, ct).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // Record NotInterested interaction (weight -2.0 handled by profile updater)
        _ = queue.TryEnqueue(new InteractionEnvelope(
            request.UserId, null, request.EntityKind, request.EntityId,
            InteractionType.NotInterested, now, null, null));

        // Invalidate recommendation cache
        await cache.RemoveByTagAsync($"analytics:recs:{request.UserId:N}", ct).ConfigureAwait(false);

        logger.LogDebug("Marked entity {EntityKind}/{EntityId} as not-interested for user {UserId}", request.EntityKind, request.EntityId, request.UserId);
        return Result.Success();
    }
}
