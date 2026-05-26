using MediatR;
using Messaging.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyDeviceTokens;

public sealed record DeviceTokenDto(Guid Id, string DeviceId, string Platform, DateTime LastSeenAt, DateTime CreatedAt);

public sealed record GetMyDeviceTokensQuery(Guid UserId) : IRequest<Result<IReadOnlyList<DeviceTokenDto>>>, ICacheableQuery
{
    public string CacheKey => MessagingCacheKeys.DeviceTokens(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [MessagingCacheKeys.DeviceTokensTag(UserId)];
}
