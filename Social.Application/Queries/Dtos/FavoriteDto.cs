using Social.Domain.Enums;

namespace Social.Application.Queries.Dtos;

/// <summary>Read projection for a single Favorite.</summary>
public sealed record FavoriteDto(
    Guid Id,
    Guid UserId,
    string EntityType,
    Guid EntityId,
    DateTime AddedAt);

/// <summary>Paginated list of Favorites.</summary>
public sealed record FavoritePageDto(
    IReadOnlyList<FavoriteDto> Items,
    Guid? NextCursor);
