using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Admin Dashboard GAP-P2 — verifies the C3 output-cache eviction contract on the
/// admin entity facades: a successful moderation write evicts the matching public
/// output-cache tag(s); a failed write evicts nothing; eviction always uses
/// <see cref="CancellationToken.None"/> so it still runs if the admin client
/// disconnected after the backend committed.
/// </summary>
public sealed class AdminFacadeCacheEvictionTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _route;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> route) => _route = route;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_route(request));
    }

    /// <summary>Captures every tag evicted plus the token argument used.</summary>
    private sealed class CapturingCacheStore : IOutputCacheStore
    {
        public List<string> EvictedTags { get; } = new();
        public List<CancellationToken> Tokens { get; } = new();

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            EvictedTags.Add(tag);
            Tokens.Add(cancellationToken);
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
            => ValueTask.FromResult<byte[]?>(null);

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private static HttpClient Http(Func<HttpRequestMessage, HttpResponseMessage> route)
        => new(new StubHandler(route)) { BaseAddress = new Uri("https://api.test/") };

    private static HttpResponseMessage Json(HttpStatusCode code, string body)
        => new(code) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    // Trips moderation write fetches the tour detail (for RowVersion) on some paths,
    // but Approve/Reject/Suspend/Reinstate post a base64 rowVersion directly. We feed
    // a generic OK for every request so the write succeeds.
    private static (TripsFacade facade, CapturingCacheStore cache) CreateTrips(
        Func<HttpRequestMessage, HttpResponseMessage> route)
    {
        var api = new TripsApiClient(new ApiClient(Http(route), Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiClient>.Instance));
        var cache = new CapturingCacheStore();
        return (new TripsFacade(api, cache), cache);
    }

    private static readonly Guid TourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    // base64 of a non-empty rowVersion ("foo")
    private const string RowVersion = "Zm9v";

    [Fact]
    public async Task Trips_Approve_Success_EvictsTourTag()
    {
        var (facade, cache) = CreateTrips(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await facade.ApproveAsync(TourId, RowVersion, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.EvictedTags.Should().ContainSingle().Which.Should().Be($"tour:{TourId}");
    }

    [Fact]
    public async Task Trips_Approve_Success_EvictsWithCancellationTokenNone()
    {
        var (facade, cache) = CreateTrips(_ => Json(HttpStatusCode.OK, "{}"));

        // Even though the action passes a live request token through to the backend
        // call, the eviction itself must run under CancellationToken.None so cache
        // consistency is durable if the admin client disconnects post-commit.
        using var cts = new CancellationTokenSource();
        await facade.ApproveAsync(TourId, RowVersion, cts.Token);

        cache.Tokens.Should().NotBeEmpty();
        cache.Tokens.Should().OnlyContain(t => t == CancellationToken.None);
    }

    [Fact]
    public async Task Trips_Approve_Failure_EvictsNothing()
    {
        var (facade, cache) = CreateTrips(_ => Json(HttpStatusCode.Conflict, """{ "title": "Tour.ConcurrencyConflict" }"""));

        var result = await facade.ApproveAsync(TourId, RowVersion, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        cache.EvictedTags.Should().BeEmpty();
    }
}
