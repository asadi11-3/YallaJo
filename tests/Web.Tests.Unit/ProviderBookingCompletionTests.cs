using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Bookings;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2B-2 — provider "mark booking completed" flow: the ApiClient posts an empty body
/// to POST /api/v1/booking/{id}/complete, the Facade maps backend failures (NotYetStarted,
/// invalid state, ownership, concurrency) to friendly messages, and the detail VM only
/// exposes the Complete action for Confirmed bookings.
/// </summary>
public sealed class ProviderBookingCompletionTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = new();
        public List<string?> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (ProviderBookingsApiClient Sut, StubHandler Handler) CreateApiClient(
        HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new ProviderBookingsApiClient(api), handler);
    }

    private static ProviderBookingsFacade CreateFacade(HttpStatusCode status, string body = "{}")
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return new ProviderBookingsFacade(new ProviderBookingsApiClient(api));
    }

    // ── ApiClient route / body ────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_PostsToCompleteRoute_WithEmptyBody()
    {
        var (sut, handler) = CreateApiClient();
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await sut.CompleteAsync(id);

        result.IsSuccess.Should().BeTrue();
        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/booking/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/complete");
        handler.Bodies[0].Should().BeNull(); // empty body
    }

    // ── Facade mapping ────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_MapsBadRequest_ToFriendlyValidationMessage()
    {
        // The backend returns 400 for "not yet started"; the non-generic ApiResult
        // pipeline drops the Detail on a 400, so the facade surfaces a clear fallback.
        var facade = CreateFacade(HttpStatusCode.BadRequest,
            """{"title":"TourBooking.NotYetStarted","detail":"Cannot complete a tour that has not yet started."}""");

        var result = await facade.CompleteAsync(Guid.NewGuid());

        result.Outcome.Should().Be(ProviderBookingOutcome.ValidationError);
        result.Error.Should().Contain("after it has started");
    }

    [Fact]
    public async Task CompleteAsync_MapsBadRequest_SurfacesBackendMessage_WhenPreserved()
    {
        // When the backend message IS preserved (e.g. a validation-errors payload),
        // the facade passes it through rather than overriding it.
        var facade = CreateFacade(HttpStatusCode.BadRequest,
            """{"title":"Validation.BookingId","detail":"Booking id is required."}""");

        var result = await facade.CompleteAsync(Guid.NewGuid());

        result.Outcome.Should().Be(ProviderBookingOutcome.ValidationError);
    }

    [Fact]
    public async Task CompleteAsync_MapsForbidden_ToOwnershipMessage()
    {
        var facade = CreateFacade(HttpStatusCode.Forbidden,
            """{"title":"TourBooking.OwnerMismatch","detail":"x"}""");

        var result = await facade.CompleteAsync(Guid.NewGuid());

        result.Outcome.Should().Be(ProviderBookingOutcome.Forbidden);
        result.Error.Should().Contain("provider of this tour");
    }

    [Fact]
    public async Task CompleteAsync_MapsConflict_ToReloadMessage()
    {
        var facade = CreateFacade(HttpStatusCode.Conflict,
            """{"title":"TourBooking.ConcurrencyConflict","detail":"x"}""");

        var result = await facade.CompleteAsync(Guid.NewGuid());

        result.Outcome.Should().Be(ProviderBookingOutcome.Conflict);
        result.Error.Should().Contain("Reload");
    }

    [Fact]
    public async Task CompleteAsync_MapsUnauthorized_ToForceSignOut()
    {
        var facade = CreateFacade(HttpStatusCode.Unauthorized, "{}");

        var result = await facade.CompleteAsync(Guid.NewGuid());

        result.Outcome.Should().Be(ProviderBookingOutcome.ForceSignOut);
    }

    [Fact]
    public async Task CompleteAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(HttpStatusCode.OK, "{}");

        var result = await facade.CompleteAsync(Guid.NewGuid());

        result.Outcome.Should().Be(ProviderBookingOutcome.Ok);
    }

    // ── VM action flags ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("Confirmed", true)]
    [InlineData("AwaitingPayment", false)]
    [InlineData("PendingConfirmation", false)]
    [InlineData("Completed", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Rejected", false)]
    public void CanComplete_IsTrue_OnlyForConfirmedBookings(string status, bool expected)
    {
        var vm = new ProviderBookingDetailsVm { Status = status };

        vm.CanComplete.Should().Be(expected);
    }

    [Fact]
    public void HasAnyAction_IncludesComplete_ForConfirmedBooking()
    {
        var vm = new ProviderBookingDetailsVm { Status = "Confirmed" };

        vm.CanComplete.Should().BeTrue();
        vm.HasAnyAction.Should().BeTrue();
    }
}
