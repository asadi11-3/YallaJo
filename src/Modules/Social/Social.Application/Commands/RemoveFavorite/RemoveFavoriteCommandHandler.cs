using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Social.Application.Caching;
using Social.Application.Interfaces;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.RemoveFavorite;

internal sealed class RemoveFavoriteCommandHandler(
    IFavoriteRepository favoriteRepository,
    ISocialUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<RemoveFavoriteCommandHandler> logger)
    : IRequestHandler<RemoveFavoriteCommand, Result>
{
    public async Task<Result> Handle(RemoveFavoriteCommand request, CancellationToken ct)
    {
        var favorite = await favoriteRepository.GetByUserAndEntityAsync(
            request.UserId, request.EntityType, request.EntityId, ct);

        // S-R7: DELETE is idempotent — not found or already removed = success (204)
        if (favorite is null || favorite.IsDeleted)
        {
            logger.LogDebug("Favorite not found for User {UserId} {EntityType}:{EntityId} — treating as already removed",
                request.UserId, request.EntityType, request.EntityId);
            return Result.Success();
        }

        favorite.Remove(timeProvider);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(SocialCacheKeys.FavoritesTag(request.UserId), ct).ConfigureAwait(false);

        logger.LogInformation("User {UserId} removed favorite for {EntityType}:{EntityId}",
            request.UserId, request.EntityType, request.EntityId);

        return Result.Success();
    }
}
