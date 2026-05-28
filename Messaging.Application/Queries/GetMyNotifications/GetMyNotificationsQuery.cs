using Messaging.Application.Caching;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Enums;
using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    Guid UserId,
    NotificationType? Type = null,
    bool? IsRead = null,
    DateTime? From = null,
    DateTime? To = null,
    Guid? AfterCursor = null,
    int PageSize = 20) : IRequest<Result<NotificationPageDto>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.Notifications(UserId, Type?.ToString(), IsRead, From, To, AfterCursor, Math.Clamp(PageSize, 1, 50));
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.NotificationsTag(UserId)];
}
