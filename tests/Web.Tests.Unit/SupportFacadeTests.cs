using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1C — verifies <see cref="SupportFacade"/>: list safe-degrade (never 500s),
/// reply/close error mapping (403/404/409/validation) and the concurrency message.
/// </summary>
public sealed class SupportFacadeTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _route;
        public List<HttpRequestMessage> Calls { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> route) => _route = route;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return Task.FromResult(_route(request));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private static SupportFacade CreateFacade(Func<HttpRequestMessage, HttpResponseMessage> route)
    {
        var handler = new StubHandler(route);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new SupportApiClient(new ApiClient(http, NullLogger<ApiClient>.Instance));
        return new SupportFacade(api, NullLogger<SupportFacade>.Instance);
    }

    private const string TwoTicketsPage = """
    {
      "items": [
        { "id": "11111111-1111-1111-1111-111111111111", "createdByUserId": "0a0a0a0a-0a0a-0a0a-0a0a-0a0a0a0a0a0a", "category": "BookingIssue", "subject": "Cancelled tour", "priority": "Normal", "status": "Open", "createdAt": "2026-01-01T10:00:00Z", "rowVersion": "Zm9v" },
        { "id": "22222222-2222-2222-2222-222222222222", "createdByUserId": "0a0a0a0a-0a0a-0a0a-0a0a-0a0a0a0a0a0a", "category": "PaymentProblem", "subject": "Double charge", "priority": "Normal", "status": "Closed", "createdAt": "2026-01-02T10:00:00Z", "rowVersion": "YmFy" }
      ],
      "nextCursor": "33333333-3333-3333-3333-333333333333"
    }
    """;

    private const string TicketDetail = """
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "createdByUserId": "0a0a0a0a-0a0a-0a0a-0a0a-0a0a0a0a0a0a",
      "category": "BookingIssue", "subject": "Cancelled tour", "priority": "Normal",
      "status": "Open", "createdAt": "2026-01-01T10:00:00Z", "rowVersion": "Zm9v",
      "messages": [
        { "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "authorUserId": "0a0a0a0a-0a0a-0a0a-0a0a-0a0a0a0a0a0a", "body": "Help please", "isInternal": false, "createdAt": "2026-01-01T11:00:00Z" },
        { "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "authorUserId": "0b0b0b0b-0b0b-0b0b-0b0b-0b0b0b0b0b0b", "body": "INTERNAL note", "isInternal": true, "createdAt": "2026-01-01T11:30:00Z" }
      ]
    }
    """;

    // ── List safe-degrade ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetListAsync_Success_MapsItemsAndCursor()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.OK, TwoTicketsPage));

        var vm = await facade.GetListAsync(cursor: null);

        vm.LoadError.Should().BeNull();
        vm.Items.Should().HaveCount(2);
        vm.NextCursor.Should().Be(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    }

    [Fact]
    public async Task GetListAsync_ApiFailure_DegradesToEmptyWithLoadError()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.InternalServerError, """{ "title": "boom" }"""));

        var vm = await facade.GetListAsync(cursor: null);

        vm.LoadError.Should().NotBeNullOrWhiteSpace();
        vm.Items.Should().BeEmpty();
        vm.HasItems.Should().BeFalse();
    }

    [Fact]
    public async Task GetListAsync_TransportException_DegradesInsteadOf500()
    {
        var facade = CreateFacade(_ => throw new HttpRequestException("network down"));

        var vm = await facade.GetListAsync(cursor: null);

        vm.LoadError.Should().NotBeNullOrWhiteSpace();
        vm.Items.Should().BeEmpty();
    }

    // ── Detail mapping + ownership/notfound friendly messages ──────────────────

    [Fact]
    public async Task GetDetailAsync_Success_HidesInternalMessages()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.OK, TicketDetail));

        var result = await facade.GetDetailAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Messages.Should().HaveCount(1, "internal staff notes are hidden from the owner");
        result.Data.Messages.Should().NotContain(m => m.Body.Contains("INTERNAL"));
    }

    [Fact]
    public async Task GetDetailAsync_Forbidden_MapsToFriendlyMessage()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.Forbidden, """{ "title": "Forbidden" }"""));

        var result = await facade.GetDetailAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error!.ToLowerInvariant().Should().Contain("access");
    }

    [Fact]
    public async Task GetDetailAsync_NotFound_MapsToFriendlyMessage()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.NotFound, """{ "title": "NotFound" }"""));

        var result = await facade.GetDetailAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error!.ToLowerInvariant().Should().Contain("not found");
    }

    // ── Reply ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostMessageAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await facade.PostMessageAsync(Guid.NewGuid(), "any update?");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PostMessageAsync_Forbidden_MapsToFriendlyMessage()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.Forbidden, """{ "title": "Forbidden" }"""));

        var result = await facade.PostMessageAsync(Guid.NewGuid(), "any update?");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error!.ToLowerInvariant().Should().Contain("access");
    }

    [Fact]
    public async Task PostMessageAsync_Validation_PassesThroughErrors()
    {
        var facade = CreateFacade(_ => Json((HttpStatusCode)422, """{ "errors": { "Body": ["Message body is required."] } }"""));

        var result = await facade.PostMessageAsync(Guid.NewGuid(), " ");

        result.IsSuccess.Should().BeFalse();
        result.IsValidationError.Should().BeTrue();
    }

    // ── Close ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await facade.CloseAsync(Guid.NewGuid(), "Zm9v");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CloseAsync_Conflict_MapsToReloadMessage()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.Conflict, """{ "title": "SupportTicket.ConcurrencyConflict" }"""));

        var result = await facade.CloseAsync(Guid.NewGuid(), "stale-rowversion");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error!.ToLowerInvariant().Should().Contain("reload");
    }

    [Fact]
    public async Task CloseAsync_NotFound_MapsToFriendlyMessage()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.NotFound, """{ "title": "NotFound" }"""));

        var result = await facade.CloseAsync(Guid.NewGuid(), "Zm9v");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error!.ToLowerInvariant().Should().Contain("not found");
    }
}
