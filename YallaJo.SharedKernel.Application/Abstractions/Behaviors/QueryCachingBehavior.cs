using System.Reflection;
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

    /// <summary>
    /// Cached <c>IsSuccess</c> property accessor for <typeparamref name="TResponse"/>.
    /// Resolved once per closed generic type so the per-request cost is a
    /// single null-check rather than a reflection lookup.  <c>null</c> when
    /// <typeparamref name="TResponse"/> has no <c>IsSuccess</c> property (i.e.
    /// the response is not a <c>Result</c> / <c>Result&lt;T&gt;</c>).
    /// </summary>
    private static readonly PropertyInfo? IsSuccessProperty =
        typeof(TResponse).GetProperty("IsSuccess", BindingFlags.Public | BindingFlags.Instance);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var key = cacheableQuery.CacheKey;
        logger.LogDebug("[Cache] Key={CacheKey}", key);

        // JWT-401 hardening: when the response type is Result/Result<T>, run the
        // handler directly and only write to cache on success.  This prevents a
        // failed Result (e.g. Auth.Unauthorized produced once when
        // HttpContext.User was momentarily null, or a validation failure on a
        // bad input) from being persisted and replayed to subsequent
        // authenticated callers for the duration of the cache TTL.
        //
        // Trade-off: when the response IS a Result, we lose HybridCache's
        // request-coalescing for the brief window between the first call and
        // the cache write.  This is an acceptable cost vs. the very real risk
        // of cache-poisoning failure responses for user-context queries
        // (GetProfileQuery, ListActiveSessionsQuery, etc.).
        if (IsSuccessProperty is not null)
        {
            // Phase 1: try to serve a *successful* cached response.  We use
            // GetOrCreateAsync with a factory that runs the handler and
            // returns the response; if the response is a failure, we evict
            // the cache entry that GetOrCreateAsync just wrote.
            var response = await cache.GetOrCreateAsync<TResponse>(
                key,
                async cancel => await next(),
                options: new HybridCacheEntryOptions
                {
                    Expiration = cacheableQuery.CacheDuration ?? DefaultDuration,
                },
                tags: cacheableQuery.Tags,
                cancellationToken: cancellationToken);

            if (response is not null
                && IsSuccessProperty.GetValue(response) is bool isSuccess
                && !isSuccess)
            {
                // Either (a) we just produced and cached a failure, or
                // (b) we just received a previously-cached failure.  In both
                // cases we evict so the *next* caller re-evaluates the handler.
                await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                logger.LogDebug(
                    "[Cache] Evicted failed response for Key={CacheKey} so subsequent calls re-evaluate the handler.",
                    key);

                // For case (b), the response we hold is the stale cached
                // failure.  Re-invoke the handler against the *current*
                // request's principal so this caller gets a fresh evaluation
                // and isn't penalised for an earlier caller's transient
                // failure.  Do NOT write the new response to the cache —
                // we've already evicted, and if the fresh response is also a
                // failure we want the same eviction-without-persist behaviour.
                response = await next();
            }

            return response!;
        }

        // Non-Result response — preserve original cache-everything behaviour.
        var nonResultResponse = await cache.GetOrCreateAsync<TResponse>(
            key,
            async cancel => await next(),
            options: new HybridCacheEntryOptions
            {
                Expiration = cacheableQuery.CacheDuration ?? DefaultDuration,
            },
            tags: cacheableQuery.Tags,
            cancellationToken: cancellationToken);

        return nonResultResponse!;
    }
}
