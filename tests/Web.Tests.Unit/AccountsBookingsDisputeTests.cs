using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;
using WebPermission = YallaJo.Web.Infrastructure.Authorization.WebPermission;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1A-1 — verifies the customer "open booking dispute" flow:
/// the ApiClient hits POST /api/v1/booking/{id}/dispute with the right body,
/// the Facade maps backend failures to friendly messages, and the permission
/// constant matches the backend-formatted string.
/// </summary>
public sealed class AccountsBookingsDisputeTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
        {
            _status = status;
            _body = body;
        }

        public List<HttpRequestMessage> Calls { get; } = new();
        public List<string?> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (BookingsApiClient Sut, StubHandler Handler) CreateApiClient(
        HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new BookingsApiClient(api), handler);
    }

    private sealed class StubAssetResolver : IApiAssetUrlResolver
    {
        public string? Resolve(string? path) => path;
    }

    private static BookingsFacade CreateFacade(HttpStatusCode status, string body = "{}")
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return new BookingsFacade(new BookingsApiClient(api), new StubAssetResolver());
    }

    // ── ApiClient URL / body ──────────────────────────────────────────────────

    [Fact]
    public async Task OpenDisputeAsync_ShouldCall_POST_DisputeRoute_WithReasonBody()
    {
        var (sut, handler) = CreateApiClient();
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await sut.OpenDisputeAsync(id, new OpenBookingDisputeRequest("The guide never showed up."));

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().HaveCount(1);
        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/booking/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/dispute");

        using var doc = JsonDocument.Parse(handler.Bodies[0]!);
        doc.RootElement.GetProperty("reason").GetString().Should().Be("The guide never showed up.");
    }

    // ── Facade error mapping ──────────────────────────────────────────────────

    [Fact]
    public async Task OpenDisputeAsync_Forbidden_MapsTo_OwnerOnlyMessage()
    {
        var facade = CreateFacade(HttpStatusCode.Forbidden, "{\"title\":\"Forbidden\"}");

        var result = await facade.OpenDisputeAsync(Guid.NewGuid(), "A valid ten+ char reason.");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("your own booking");
    }

    [Fact]
    public async Task OpenDisputeAsync_UnprocessableEntity_MapsTo_WindowMessage()
    {
        var facade = CreateFacade((HttpStatusCode)422, "{\"title\":\"TourBooking.InvalidState\"}");

        var result = await facade.OpenDisputeAsync(Guid.NewGuid(), "A valid ten+ char reason.");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OpenDisputeAsync_Conflict_MapsTo_ReloadMessage()
    {
        var facade = CreateFacade(HttpStatusCode.Conflict, "{\"title\":\"TourBooking.ConcurrencyConflict\"}");

        var result = await facade.OpenDisputeAsync(Guid.NewGuid(), "A valid ten+ char reason.");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error!.ToLowerInvariant().Should().Contain("reload");
    }

    [Fact]
    public async Task OpenDisputeAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(HttpStatusCode.OK);

        var result = await facade.OpenDisputeAsync(Guid.NewGuid(), "A valid ten+ char reason.");

        result.IsSuccess.Should().BeTrue();
    }

    // ── Permission constant ───────────────────────────────────────────────────

    [Fact]
    public void BookingDispute_permission_constants_match_backend_format()
    {
        WebPermission.BookingDispute.Create.Should().Be("Permission.BookingDispute.Create");
        WebPermission.BookingDispute.Resolve.Should().Be("Permission.BookingDispute.Resolve");
        WebPermission.BookingDispute.Read.Should().Be("Permission.BookingDispute.Read");
    }

    // ── Mapper: IsDisputable window logic ──────────────────────────────────────

    [Fact]
    public void IsDisputable_True_When_Completed_Within48h_And_NotDisputed()
    {
        BookingsMapper.IsDisputable("Completed", DateTime.UtcNow.AddHours(-1), alreadyDisputed: false)
            .Should().BeTrue();
    }

    [Fact]
    public void IsDisputable_False_When_Completed_Older_Than48h()
    {
        BookingsMapper.IsDisputable("Completed", DateTime.UtcNow.AddHours(-49), alreadyDisputed: false)
            .Should().BeFalse();
    }

    [Fact]
    public void IsDisputable_False_When_NotCompleted()
    {
        BookingsMapper.IsDisputable("Confirmed", DateTime.UtcNow.AddHours(-1), alreadyDisputed: false)
            .Should().BeFalse();
    }

    [Fact]
    public void IsDisputable_False_When_AlreadyDisputed()
    {
        BookingsMapper.IsDisputable("Completed", DateTime.UtcNow.AddHours(-1), alreadyDisputed: true)
            .Should().BeFalse();
    }
}
