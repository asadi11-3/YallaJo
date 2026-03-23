using Microsoft.Extensions.Caching.Hybrid;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for queries that support in-memory result caching.
/// Implement on IQuery records to opt into the <see cref="Behaviors.QueryCachingBehavior{TRequest,TResponse}"/> pipeline.
/// </summary>
/// <remarks>
/// Cache invalidation: command handlers that mutate the related data must call
/// <c>HybridCache.RemoveByTagAsync(tag, ct)</c> for the affected tags after saving.
/// </remarks>
public interface ICacheableQuery
{
    /// <summary>
    /// Unique, deterministic cache key derived from the query's parameters.
    /// Use the module's static <c>CacheKeys</c> class to build this consistently.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// How long the cached result is valid. <c>null</c> uses the behavior default (15 minutes).
    /// </summary>
    TimeSpan? CacheDuration { get; }

    /// <summary>
    /// Cache tags used for group-based invalidation.
    /// </summary>
    IReadOnlyList<string> Tags => [];
}
