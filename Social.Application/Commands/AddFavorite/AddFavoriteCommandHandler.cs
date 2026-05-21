using MediatR;
using Microsoft.Extensions.Logging;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using Social.Application.Interfaces;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AddFavorite;

internal sealed class AddFavoriteCommandHandler(
    IFavoriteRepository favoriteRepository,
    ISocialUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<AddFavoriteCommandHandler> logger)
    : IRequestHandler<AddFavoriteCommand, Result<Guid>>
{
    private const int MaxFavoritesPerUser = 500;

    public async Task<Result<Guid>> Handle(AddFavoriteCommand request, CancellationToken ct)
    {
        // S-R7: max 500 favorites per user
        var count = await favoriteRepository.CountByUserAsync(request.UserId, ct);
        if (count >= MaxFavoritesPerUser)
        {
            logger.LogWarning("User {UserId} has reached the favorite limit of {Max}", request.UserId, MaxFavoritesPerUser);
            return Result.Failure<Guid>(
                new Error("Favorite.LimitReached", $"You have reached the maximum of {MaxFavoritesPerUser} favorites."),
                Outcome.Invalid);
        }

        // S-R7: idempotency — reject duplicate (409)
        var existing = await favoriteRepository.GetByUserAndEntityAsync(
            request.UserId, request.EntityType, request.EntityId, ct);

        if (existing is not null && !existing.IsDeleted)
        {
            return Result.Failure<Guid>(
                new Error("Favorite.AlreadyExists", "You have already favorited this item."),
                Outcome.Conflict);
        }

        // Create new (or re-create after removal)
        var favorite = Favorite.Add(request.UserId, request.EntityType, request.EntityId, timeProvider);
        await favoriteRepository.AddAsync(favorite, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} added favorite {FavoriteId} for {EntityType}:{EntityId}",
            request.UserId, favorite.Id, request.EntityType, request.EntityId);

        return Result.Success(favorite.Id);
    }
}
