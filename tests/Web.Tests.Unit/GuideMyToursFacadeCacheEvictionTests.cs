using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.MyTours;
using YallaJo.Web.Areas.Guide.Services;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// GAP-G19 — verifies <see cref="GuideMyToursFacade"/> evicts the public
/// output-cache tag <c>tour:{tourId}</c> after each successful offering write,
/// using <see cref="CancellationToken.None"/> (so eviction survives client
/// disconnect after the backend committed), and NEVER evicts on failure.
/// </summary>
public sealed class GuideMyToursFacadeCacheEvictionTests
{
    private static readonly Guid TourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GuideId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ScheduleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TierId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private const string ProfilePath = "/api/v1/guides/me";

    private static readonly string ProfileJson = $"{{\"id\":\"{GuideId}\"}}";

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _route;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> route) => _route = route;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_route(request));
    }

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

    private static HttpResponseMessage Json(HttpStatusCode code, string body)
        => new(code) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private sealed class StubCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.Parse("55555555-5555-5555-5555-555555555555");
        public bool HasPermission(string permission) => true;
        public bool IsInRole(string role) => true;
    }

    private static GuideIdAccessor CreateAccessor(ApiClient api) => new(
        new DashboardApiClient(api),
        new MemoryCache(new MemoryCacheOptions()),
        new StubCurrentUser(),
        NullLogger<GuideIdAccessor>.Instance);

    private static (GuideMyToursFacade Facade, CapturingCacheStore Cache) BuildFacade(
        Func<HttpRequestMessage, HttpResponseMessage> writeRoute)
    {
        // Route the my-profile GET to a valid profile (so the guide id resolves),
        // and everything else to the caller-supplied write route under test.
        HttpResponseMessage Route(HttpRequestMessage request)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith(ProfilePath, StringComparison.OrdinalIgnoreCase))
            {
                return Json(HttpStatusCode.OK, ProfileJson);
            }

            return writeRoute(request);
        }

        var http = new HttpClient(new StubHandler(Route)) { BaseAddress = new Uri("https://api.test/") };
        var cache = new CapturingCacheStore();
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        var facade = new GuideMyToursFacade(
            new MyToursApiClient(api),
            CreateAccessor(api),
            cache,
            NullLogger<GuideMyToursFacade>.Instance);
        return (facade, cache);
    }

    private static AddScheduleFormVm ScheduleForm() => new()
    {
        DayOfWeek = 1,
        StartTime = "09:00",
        EndTime = "17:00",
    };

    private static AddPricingTierFormVm PricingForm() => new()
    {
        Name = "Standard",
        Price = 50m,
        Currency = "JOD",
        MinParticipants = 1,
        MaxParticipants = 10,
        Description = null,
    };

    private static PrivateTourFormVm PrivateForm() => new()
    {
        Multiplier = 1.5m,
        FlatPrice = null,
    };

    public static IEnumerable<object[]> Writes()
    {
        yield return new object[] { "add-schedule", (Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>>)(f => f.AddScheduleAsync(TourId, ScheduleForm(), CancellationToken.None)) };
        yield return new object[] { "delete-schedule", (Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>>)(f => f.DeleteScheduleAsync(TourId, ScheduleId, CancellationToken.None)) };
        yield return new object[] { "add-pricing", (Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>>)(f => f.AddPricingTierAsync(TourId, PricingForm(), CancellationToken.None)) };
        yield return new object[] { "delete-pricing", (Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>>)(f => f.DeletePricingTierAsync(TourId, TierId, CancellationToken.None)) };
        yield return new object[] { "enable-private", (Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>>)(f => f.EnablePrivateTourAsync(TourId, PrivateForm(), CancellationToken.None)) };
        yield return new object[] { "disable-private", (Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>>)(f => f.DisablePrivateTourAsync(TourId, CancellationToken.None)) };
        yield return new object[] { "remove-offering", (Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>>)(f => f.RemoveOfferingAsync(TourId, CancellationToken.None)) };
    }

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task Write_WhenBackendSucceeds_EvictsTourTagOnce(
        string name,
        Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>> act)
    {
        _ = name;
        var (facade, cache) = BuildFacade(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await act(facade);

        result.IsSuccess.Should().BeTrue();
        cache.EvictedTags.Should().ContainSingle().Which.Should().Be($"tour:{TourId}");
        cache.Tokens.Should().OnlyContain(t => t == CancellationToken.None);
    }

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task Write_WhenBackendFails_DoesNotEvict(
        string name,
        Func<GuideMyToursFacade, Task<YallaJo.Web.Infrastructure.Api.Contracts.ApiResult>> act)
    {
        _ = name;
        var (facade, cache) = BuildFacade(_ => Json(HttpStatusCode.Conflict, "{}"));

        var result = await act(facade);

        result.IsSuccess.Should().BeFalse();
        cache.EvictedTags.Should().BeEmpty();
    }

    [Fact]
    public async Task Write_WhenGuideProfileUnresolvable_DoesNotEvict()
    {
        // Profile GET returns non-success → guide id cannot resolve → no write, no eviction.
        var http = new HttpClient(new StubHandler(_ => Json(HttpStatusCode.NotFound, "{}")))
        {
            BaseAddress = new Uri("https://api.test/"),
        };
        var cache = new CapturingCacheStore();
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        var facade = new GuideMyToursFacade(
            new MyToursApiClient(api),
            CreateAccessor(api),
            cache,
            NullLogger<GuideMyToursFacade>.Instance);

        var result = await facade.RemoveOfferingAsync(TourId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        cache.EvictedTags.Should().BeEmpty();
    }
}
