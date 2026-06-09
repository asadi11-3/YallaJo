using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Coverage for the Home "Check Availability" form integration:
///   – <c>placeId</c> is plumbed through to the search API query string (backend-supported).
///   – <c>from / to / participants</c> are echoed on the VM but NOT sent to the API
///     (no availability-window filter on the backend yet).
///   – Participants is clamped to <c>[1, 50]</c>; an inverted date range is auto-swapped;
///     <see cref="Guid.Empty"/> placeId is normalized to <c>null</c>.
///   – The facade never throws: API failure returns an empty-but-echoed VM.
/// </summary>
public sealed class SearchFacadeTests
{
    // ── Stub that captures the outbound URL so we can assert query plumbing ────
    private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static (SearchFacade facade, CapturingHandler handler) CreateFacade(
        HttpStatusCode status = HttpStatusCode.OK,
        string body = """{"items":[],"pageNumber":1,"pageSize":20,"totalCount":0,"hasPreviousPage":false,"hasNextPage":false}""")
    {
        var handler = new CapturingHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new SearchFacade(new SearchApiClient(api)), handler);
    }

    // ── placeId plumbing ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_PassesPlaceId_AsApiQueryParameter()
    {
        var (facade, handler) = CreateFacade();
        var placeId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await facade.SearchAsync(query: null, placeId: placeId,
            from: null, to: null, participants: null, page: 1, pageSize: 20);

        result.IsSuccess.Should().BeTrue();
        handler.LastUri.Should().NotBeNull();
        handler.LastUri!.Query.Should().Contain($"placeId={placeId}");
    }

    [Fact]
    public async Task SearchAsync_GuidEmptyPlaceId_IsNormalizedToNull_AndNotSentToApi()
    {
        var (facade, handler) = CreateFacade();

        var result = await facade.SearchAsync(query: null, placeId: Guid.Empty,
            from: null, to: null, participants: null, page: 1, pageSize: 20);

        result.Data!.PlaceId.Should().BeNull();
        handler.LastUri!.Query.Should().NotContain("placeId=");
    }

    // ── from / to / participants are echoed but NOT sent to API ───────────────

    [Fact]
    public async Task SearchAsync_AvailabilityFilters_AreEchoedOnVm_ButNotSentToApi()
    {
        var (facade, handler) = CreateFacade();
        var from = new DateOnly(2026, 03, 14);
        var to = new DateOnly(2026, 03, 20);

        var result = await facade.SearchAsync(query: null, placeId: null,
            from: from, to: to, participants: 4, page: 1, pageSize: 20);

        // Echoed on VM
        result.Data!.From.Should().Be(from);
        result.Data!.To.Should().Be(to);
        result.Data!.Participants.Should().Be(4);
        result.Data!.HasFilters.Should().BeTrue();

        // NOT sent to API (backend has no availability filter yet)
        var qs = handler.LastUri!.Query;
        qs.Should().NotContain("from=");
        qs.Should().NotContain("to=");
        qs.Should().NotContain("participants=");
    }

    // ── Participants clamping ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 1)]    // below min → clamped to 1
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(50, 50)]
    [InlineData(99, 50)]  // above max → clamped to 50
    public async Task SearchAsync_ParticipantsClampedTo1To50(int input, int expected)
    {
        var (facade, _) = CreateFacade();

        var result = await facade.SearchAsync(query: null, placeId: null,
            from: null, to: null, participants: input, page: 1, pageSize: 20);

        result.Data!.Participants.Should().Be(expected);
    }

    [Fact]
    public async Task SearchAsync_NullParticipants_StaysNull()
    {
        var (facade, _) = CreateFacade();

        var result = await facade.SearchAsync(query: null, placeId: null,
            from: null, to: null, participants: null, page: 1, pageSize: 20);

        result.Data!.Participants.Should().BeNull();
    }

    // ── Inverted date range gets swapped ──────────────────────────────────────

    [Fact]
    public async Task SearchAsync_InvertedDateRange_IsSwapped()
    {
        var (facade, _) = CreateFacade();
        var later = new DateOnly(2026, 03, 20);
        var earlier = new DateOnly(2026, 03, 14);

        // User accidentally swaps from/to — facade normalizes so chips/URL stay meaningful.
        var result = await facade.SearchAsync(query: null, placeId: null,
            from: later, to: earlier, participants: null, page: 1, pageSize: 20);

        result.Data!.From.Should().Be(earlier);
        result.Data!.To.Should().Be(later);
    }

    // ── HasFilters reflects every dimension ───────────────────────────────────

    [Fact]
    public async Task SearchAsync_HasFilters_FalseOnEmptyForm()
    {
        var (facade, _) = CreateFacade();

        var result = await facade.SearchAsync(query: null, placeId: null,
            from: null, to: null, participants: null, page: 1, pageSize: 20);

        result.Data!.HasFilters.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_HasFilters_FalseWhenParticipantsExactlyOne()
    {
        // Solo traveler is the implicit default; we don't want a noisy "1 traveler" chip.
        var (facade, _) = CreateFacade();

        var result = await facade.SearchAsync(query: null, placeId: null,
            from: null, to: null, participants: 1, page: 1, pageSize: 20);

        result.Data!.HasFilters.Should().BeFalse();
    }

    // ── Best-effort SSR contract: API failure ≠ thrown exception ──────────────

    [Fact]
    public async Task SearchAsync_OnApiFailure_ReturnsEmptyVm_WithEchoedFilters()
    {
        var (facade, _) = CreateFacade(HttpStatusCode.InternalServerError, "boom");
        var placeId = Guid.NewGuid();

        var result = await facade.SearchAsync(query: "hike", placeId: placeId,
            from: new DateOnly(2026, 04, 01), to: new DateOnly(2026, 04, 03),
            participants: 3, page: 2, pageSize: 25);

        // Still Ok — facade never bubbles failures to the SSR controller.
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();

        // Echoed filters survive the failure so the page can re-render the form sticky.
        result.Data!.Query.Should().Be("hike");
        result.Data!.PlaceId.Should().Be(placeId);
        result.Data!.From.Should().Be(new DateOnly(2026, 04, 01));
        result.Data!.To.Should().Be(new DateOnly(2026, 04, 03));
        result.Data!.Participants.Should().Be(3);
        result.Data!.PageNumber.Should().Be(2);
        result.Data!.PageSize.Should().Be(25);
    }

    // ── Page-size clamp survives the new filter signature ─────────────────────

    [Fact]
    public async Task SearchAsync_PageSizeClampedToMax()
    {
        // The mock API echoes a hard-coded pageSize=20 back in the body, so we
        // can't assert on the VM (that would only verify what the API said, not
        // what the facade sent). The contract being tested is: the facade must
        // clamp pageSize before it hits the wire. Assert on the outgoing URL.
        var (facade, handler) = CreateFacade();

        var result = await facade.SearchAsync(query: null, placeId: null,
            from: null, to: null, participants: null, page: 1, pageSize: 9999);

        result.IsSuccess.Should().BeTrue();
        handler.LastUri!.Query.Should().Contain($"pageSize={SearchFacade.MaxPageSize}");
    }
}
