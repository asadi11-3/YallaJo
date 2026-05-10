using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Behaviors;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace SharedKernel.Tests.Unit;

/// <summary>
/// JWT-401 hardening regression tests for <see cref="QueryCachingBehavior{TRequest, TResponse}"/>.
///
/// <para>
/// The behaviour wraps cacheable queries in <see cref="HybridCache"/>.
/// Originally it cached every response — including failed <see cref="Result"/>
/// objects.  This caused user-context queries (GetProfileQuery,
/// ListActiveSessionsQuery, etc.) to cache an <c>Auth.Unauthorized</c> failure
/// produced under a momentarily-missing principal and replay it to subsequent
/// authenticated callers for the cache TTL — manifesting as a stuck 401.
/// </para>
///
/// <para>
/// The hardened behaviour evicts failed <see cref="Result"/> /
/// <see cref="Result{T}"/> responses immediately after producing/reading them
/// and re-invokes the handler so the *current* caller gets a fresh evaluation.
/// </para>
/// </summary>
public sealed class QueryCachingBehaviorTests
{
    // ── Test fixtures ────────────────────────────────────────────────────────

    private static HybridCache BuildCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<HybridCache>();
    }

    private static QueryCachingBehavior<TestQuery, Result<string>> BuildBehavior(HybridCache cache) =>
        new(cache, NullLogger<QueryCachingBehavior<TestQuery, Result<string>>>.Instance);

    private sealed record TestQuery(string Key, TimeSpan? Duration = null)
        : IRequest<Result<string>>, ICacheableQuery
    {
        public string CacheKey => Key;
        public TimeSpan? CacheDuration => Duration;
        public IReadOnlyList<string> Tags => [];
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryCachingBehavior_CachesSuccessfulResult()
    {
        var cache = BuildCache();
        var behavior = BuildBehavior(cache);
        var query = new TestQuery($"key-{Guid.NewGuid():N}");

        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            callCount++;
            return Task.FromResult(Result<string>.Success($"value-{callCount}"));
        };

        var first = await behavior.Handle(query, next, CancellationToken.None);
        var second = await behavior.Handle(query, next, CancellationToken.None);

        callCount.Should().Be(1,
            "successful results must be cached so the handler is invoked at most once");
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value.Should().Be(second.Value,
            "the second call must return the cached payload from the first call");
    }

    [Fact]
    public async Task QueryCachingBehavior_DoesNotCacheFailedResult()
    {
        var cache = BuildCache();
        var behavior = BuildBehavior(cache);
        var query = new TestQuery($"key-{Guid.NewGuid():N}");

        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            callCount++;
            return Task.FromResult(Result<string>.Unauthorized($"failure-{callCount}"));
        };

        var first = await behavior.Handle(query, next, CancellationToken.None);
        var second = await behavior.Handle(query, next, CancellationToken.None);
        var third = await behavior.Handle(query, next, CancellationToken.None);

        // The behaviour evicts a failed cached response and re-runs the handler
        // for the CURRENT caller.  So the inner factory may run multiple times
        // per request — we only require: the handler is invoked AT LEAST ONCE
        // per request, AND the response is always a fresh failure (not stale).
        callCount.Should().BeGreaterOrEqualTo(3,
            "failed results must NOT be cached; every request must re-invoke the handler");

        first.IsSuccess.Should().BeFalse();
        second.IsSuccess.Should().BeFalse();
        third.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task QueryCachingBehavior_AfterFailure_NextSuccessfulCallReceivesFreshResponse()
    {
        // Critical JWT-401 scenario: a previous handler invocation produced a
        // failure that was either (a) cached and is now being read back, or
        // (b) just produced and would otherwise be cached.  Subsequent calls
        // must NEVER receive the stale failure — the behaviour must evict it
        // AND re-run the handler so the current caller's principal is honoured.
        //
        // Under the hardened behaviour the eviction-and-retry happens inside
        // the SAME Handle call: the moment a failure is detected, the entry is
        // dropped and `next()` is invoked once more.  So the FIRST request
        // immediately benefits — even if its initial factory invocation
        // produced the stale failure (e.g. read from a previously-poisoned
        // cache), the retry within the same Handle call surfaces the fresh
        // result.
        var cache = BuildCache();
        var behavior = BuildBehavior(cache);
        var query = new TestQuery($"key-{Guid.NewGuid():N}");

        var attempt = 0;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            attempt++;
            return Task.FromResult(attempt == 1
                ? Result<string>.Unauthorized("first invocation had no principal")
                : Result<string>.Success($"success-on-attempt-{attempt}"));
        };

        var first = await behavior.Handle(query, next, CancellationToken.None);

        attempt.Should().Be(2,
            "the behaviour must detect the failure on attempt 1, evict, and re-invoke the handler in the same Handle call");
        first.IsSuccess.Should().BeTrue(
            "the eviction-and-retry path must surface the fresh successful response within the same call");
        first.Value.Should().Be("success-on-attempt-2",
            "the response must come from the second handler invocation, not the cached failure");
    }

    [Fact]
    public async Task QueryCachingBehavior_AfterSuccess_FollowupCallsServeCachedSuccess()
    {
        // Counterpart to the previous test: once a successful result is
        // cached, subsequent callers must see the cached payload (we did NOT
        // accidentally evict on success).
        var cache = BuildCache();
        var behavior = BuildBehavior(cache);
        var query = new TestQuery($"key-{Guid.NewGuid():N}");

        var attempt = 0;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            attempt++;
            return Task.FromResult(Result<string>.Success($"v-{attempt}"));
        };

        var first = await behavior.Handle(query, next, CancellationToken.None);
        var second = await behavior.Handle(query, next, CancellationToken.None);
        var third = await behavior.Handle(query, next, CancellationToken.None);

        attempt.Should().Be(1, "successful results must be cached and reused");
        first.Value.Should().Be("v-1");
        second.Value.Should().Be("v-1");
        third.Value.Should().Be("v-1");
    }

    [Fact]
    public async Task QueryCachingBehavior_BypassesCache_WhenRequestIsNotCacheable()
    {
        var cache = BuildCache();
        // Build a behavior with a query type that does NOT implement ICacheableQuery.
        var behavior = new QueryCachingBehavior<NonCacheableQuery, Result<string>>(
            cache,
            NullLogger<QueryCachingBehavior<NonCacheableQuery, Result<string>>>.Instance);

        var callCount = 0;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            callCount++;
            return Task.FromResult(Result<string>.Success($"v-{callCount}"));
        };

        var first = await behavior.Handle(new NonCacheableQuery(), next, CancellationToken.None);
        var second = await behavior.Handle(new NonCacheableQuery(), next, CancellationToken.None);

        callCount.Should().Be(2,
            "non-cacheable queries must invoke the handler every time");
        first.Value.Should().Be("v-1");
        second.Value.Should().Be("v-2");
    }

    private sealed record NonCacheableQuery : IRequest<Result<string>>;
}
