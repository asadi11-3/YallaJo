using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1A-2 / FE-1A-3 — verifies admin booking-dispute resolution:
/// the ApiClient hits the correct routes, and the Facade orchestrates the
/// two-step resolve(+optional refund) sequence with correct partial-failure handling.
/// </summary>
public sealed class AdminBookingsResolveDisputeTests
{
    /// <summary>
    /// Routes responses by request path so we can simulate the resolve call
    /// succeeding while the refund call fails (the critical partial-failure case).
    /// </summary>
    private sealed class RoutingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _route;
        public List<HttpRequestMessage> Calls { get; } = new();
        public List<string?> Bodies { get; } = new();

        public RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> route) => _route = route;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return _route(request);
        }
    }

    private static HttpResponseMessage Ok() =>
        new(HttpStatusCode.OK) { Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Status(HttpStatusCode code) =>
        new(code) { Content = new StringContent("{\"title\":\"err\"}", System.Text.Encoding.UTF8, "application/json") };

    private static (AdminBookingsApiClient Api, RoutingHandler Handler) CreateApiClient(
        Func<HttpRequestMessage, HttpResponseMessage> route)
    {
        var handler = new RoutingHandler(route);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        return (new AdminBookingsApiClient(new ApiClient(http, NullLogger<ApiClient>.Instance)), handler);
    }

    private static AdminBookingsFacade CreateFacade(Func<HttpRequestMessage, HttpResponseMessage> route)
    {
        var (api, _) = CreateApiClient(route);
        return new AdminBookingsFacade(api);
    }

    private static readonly Guid BookingId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PaymentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static bool IsResolvePath(HttpRequestMessage r) =>
        r.RequestUri!.AbsolutePath.Contains("/dispute/resolve");

    private static bool IsRefundPath(HttpRequestMessage r) =>
        r.RequestUri!.AbsolutePath.Contains("/refund");

    // ── ApiClient URL / body ──────────────────────────────────────────────────

    [Fact]
    public async Task ResolveDisputeAsync_ShouldCall_AdminResolveRoute_WithNotesBody()
    {
        var (api, handler) = CreateApiClient(_ => Ok());

        await api.ResolveDisputeAsync(BookingId, new ResolveBookingDisputeRequest("Looked into it; refunded."));

        handler.Calls.Should().HaveCount(1);
        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be($"/api/v1/booking/admin/{BookingId}/dispute/resolve");

        using var doc = JsonDocument.Parse(handler.Bodies[0]!);
        doc.RootElement.GetProperty("resolutionNotes").GetString().Should().Be("Looked into it; refunded.");
    }

    [Fact]
    public async Task RefundPaymentAsync_ShouldCall_PaymentRefundRoute_WithAmountBody()
    {
        var (api, handler) = CreateApiClient(_ => Ok());

        await api.RefundPaymentAsync(PaymentId, new RefundPaymentRequest(90m, "JOD", "Dispute resolution"));

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be($"/api/v1/payments/{PaymentId}/refund");

        using var doc = JsonDocument.Parse(handler.Bodies[0]!);
        doc.RootElement.GetProperty("amount").GetDecimal().Should().Be(90m);
        doc.RootElement.GetProperty("currency").GetString().Should().Be("JOD");
    }

    // ── Facade: resolve only ──────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_WithoutRefund_CallsResolveOnly_AndReturnsResolvedNoRefund()
    {
        var calls = new List<string>();
        var facade = CreateFacade(r =>
        {
            calls.Add(r.RequestUri!.AbsolutePath);
            return Ok();
        });

        var outcome = await facade.ResolveDisputeAsync(BookingId, "Resolved without refund.", refund: null);

        outcome.Kind.Should().Be(ResolveDisputeOutcome.OutcomeKind.ResolvedNoRefund);
        calls.Should().ContainSingle().Which.Should().Contain("/dispute/resolve");
        calls.Should().NotContain(c => c.Contains("/refund"));
    }

    [Fact]
    public async Task Resolve_Fails_RefundIsNeverCalled()
    {
        var calls = new List<string>();
        var facade = CreateFacade(r =>
        {
            calls.Add(r.RequestUri!.AbsolutePath);
            return IsResolvePath(r) ? Status(HttpStatusCode.Conflict) : Ok();
        });

        var refund = new RefundRequest(PaymentId, 90m, "JOD", "x");
        var outcome = await facade.ResolveDisputeAsync(BookingId, "Try to resolve.", refund);

        outcome.Kind.Should().Be(ResolveDisputeOutcome.OutcomeKind.ResolveFailed);
        outcome.IsResolved.Should().BeFalse();
        calls.Should().NotContain(c => c.Contains("/refund"),
            "the refund must never be attempted when the resolve step fails");
    }

    // ── Facade: resolve + refund ──────────────────────────────────────────────

    [Fact]
    public async Task Resolve_Succeeds_Refund_Fails_ReturnsPartialFailure_WithWarning()
    {
        var facade = CreateFacade(r =>
            IsRefundPath(r) ? Status(HttpStatusCode.BadGateway) : Ok());

        var refund = new RefundRequest(PaymentId, 90m, "JOD", "Dispute resolution");
        var outcome = await facade.ResolveDisputeAsync(BookingId, "Resolved, refund attempted.", refund);

        outcome.Kind.Should().Be(ResolveDisputeOutcome.OutcomeKind.ResolvedButRefundFailed);
        outcome.IsResolved.Should().BeTrue("the dispute WAS resolved even though the refund failed");
        outcome.Message.Should().NotBeNullOrWhiteSpace();
        outcome.Message!.ToLowerInvariant().Should().Contain("gateway");
    }

    [Fact]
    public async Task Resolve_Succeeds_Refund_Succeeds_ReturnsResolvedAndRefunded()
    {
        var calls = new List<string>();
        var facade = CreateFacade(r =>
        {
            calls.Add(r.RequestUri!.AbsolutePath);
            return Ok();
        });

        var refund = new RefundRequest(PaymentId, 90m, "JOD", "Dispute resolution");
        var outcome = await facade.ResolveDisputeAsync(BookingId, "Resolved and refunded.", refund);

        outcome.Kind.Should().Be(ResolveDisputeOutcome.OutcomeKind.ResolvedAndRefunded);
        calls.Should().Contain(c => c.Contains("/dispute/resolve"));
        calls.Should().Contain(c => c.Contains("/refund"));
    }

    [Fact]
    public async Task Resolve_Unauthorized_ReturnsSignOut()
    {
        var facade = CreateFacade(_ => Status(HttpStatusCode.Unauthorized));

        var outcome = await facade.ResolveDisputeAsync(BookingId, "Resolve attempt.", refund: null);

        outcome.RequireSignOut.Should().BeTrue();
    }
}
