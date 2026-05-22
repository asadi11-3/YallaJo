using MediatR;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.RemoveFavorite;

/// <summary>Removes a favorite entry (idempotent — 204 if not found).</summary>
public sealed record RemoveFavoriteCommand(
    Guid UserId,
    FavoriteEntityType EntityType,
    Guid EntityId) : IRequest<Result>;
