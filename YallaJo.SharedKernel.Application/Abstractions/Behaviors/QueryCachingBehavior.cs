using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace YallaJo.SharedKernel.Application.Abstractions.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches query results in <see cref="IMemoryCache"/>.
/// Activated only for requests that implement <see cref="ICacheableQuery"/>.
/// Position in pipeline: after Validation and Logging, immediately before the handler.
/// </summary>
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    IMemoryCache cache,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(15);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only activate for queries that opt into caching
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var key = cacheableQuery.CacheKey;

        if (cache.TryGetValue(key, out TResponse? cached) && cached is not null)
        {
            logger.LogDebug("[Cache HIT] {CacheKey}", key);
            return cached;
        }

        logger.LogDebug("[Cache MISS] {CacheKey} — executing handler", key);
        var response = await next();

        if (response is not null)
        {
            cache.Set(key, response, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheableQuery.CacheDuration ?? DefaultDuration,
                Priority = CacheItemPriority.Normal
            });
        }

        return response;
    }
}
