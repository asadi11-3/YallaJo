using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace YallaJo.SharedKernel.Application.Abstractions.Behaviors;

public sealed class QueryCachingBehavior<TRequest, TResponse>(
    HybridCache cache,
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
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var key = cacheableQuery.CacheKey;
        logger.LogDebug("[Cache] Key={CacheKey}", key);

        var response = await cache.GetOrCreateAsync<TResponse>(
            key,
            async cancel => await next(),
            options: new HybridCacheEntryOptions
            {
                Expiration = cacheableQuery.CacheDuration ?? DefaultDuration,
            },
            tags: cacheableQuery.Tags,
            cancellationToken: cancellationToken);

        return response!;
    }
}
