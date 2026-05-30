using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid Id, Guid CallerUserId) : IRequest<Result<NotificationDto>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.Notification(Id, CallerUserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.NotificationsTag(CallerUserId)];
}
