using MediatR;
using Messaging.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyPreferences;

public sealed record NotificationPreferenceDto(string Type, string Channel, bool IsEnabled);

public sealed record GetMyPreferencesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<NotificationPreferenceDto>>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.NotificationPreferences(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.NotificationPreferencesTag(UserId)];
}
