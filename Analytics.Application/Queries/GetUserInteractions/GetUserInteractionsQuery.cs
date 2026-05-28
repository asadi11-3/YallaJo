using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetUserInteractions;

public sealed record GetUserInteractionsQuery(Guid UserId, long? AfterId, int PageSize) : IQuery<CursorPageDto<UserInteractionDto>>, ICacheableQuery
{
    public string CacheKey => $"analytics:interactions:user:{UserId}:{AfterId}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => [$"analytics:interactions:user:{UserId}"];
}
