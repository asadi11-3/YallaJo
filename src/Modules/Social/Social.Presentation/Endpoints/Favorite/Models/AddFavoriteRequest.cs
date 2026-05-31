using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.Favorite.Models;

/// <summary>Request body for POST /favorites.</summary>
internal sealed record AddFavoriteRequest(FavoriteEntityType EntityType, Guid EntityId);
