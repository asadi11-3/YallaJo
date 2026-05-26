using MediatR;
using Messaging.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<Result<int>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.UnreadCount(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.NotificationsTag(UserId)];
}
