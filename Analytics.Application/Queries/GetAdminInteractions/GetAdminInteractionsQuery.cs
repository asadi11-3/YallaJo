using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetAdminInteractions;

public sealed record GetAdminInteractionsQuery(Guid? UserId, string? EntityType, Guid? EntityId, string? InteractionType, DateTime? From, DateTime? To, long? AfterId, int PageSize) : IQuery<CursorPageDto<UserInteractionDto>>, ICacheableQuery
{
    public string CacheKey => $"analytics:interactions:{UserId}:{EntityType}:{EntityId}:{InteractionType}:{From:o}:{To:o}:{AfterId}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["analytics:interactions"];
}
