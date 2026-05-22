using MediatR;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.CheckFavorite;

/// <summary>Returns whether the caller has favorited a given entity.</summary>
public sealed record CheckFavoriteQuery(
    Guid UserId,
    FavoriteEntityType EntityType,
    Guid EntityId) : IRequest<Result<bool>>;
