using MediatR;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.AddFavorite;

/// <summary>Adds a new favorite entry for the calling user.</summary>
/// <param name="UserId">The caller's user id.</param>
/// <param name="EntityType">Tour, Place, or Business.</param>
/// <param name="EntityId">The target entity id.</param>
public sealed record AddFavoriteCommand(
    Guid UserId,
    FavoriteEntityType EntityType,
    Guid EntityId) : IRequest<Result<Guid>>;
