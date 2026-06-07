using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1D — verifies <see cref="PrivacyFacade"/> outcome mapping: export success,
/// request/cancel happy paths, the AlreadyPending (400) and NotFound (404) friendly
/// messages (there is no pending-status read endpoint), and force-sign-out on 401.
/// </summary>
public sealed class PrivacyFacadeTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _route;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> route) => _route = route;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_route(request));
    }

    private static HttpResponseMessage Resp(HttpStatusCode code, string body, string contentType = "application/json") =>
        new(code) { Content = new StringContent(body, System.Text.Encoding.UTF8, contentType) };

    private static PrivacyFacade CreateFacade(Func<HttpRequestMessage, HttpResponseMessage> route)
    {
        var handler = new StubHandler(route);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new PrivacyApiClient(new ApiClient(http, NullLogger<ApiClient>.Instance));
        return new PrivacyFacade(api, NullLogger<PrivacyFacade>.Instance);
    }

    // ── Export ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_Success_ReturnsFileBytes()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.OK,
            """{ "userId": "11111111-1111-1111-1111-111111111111", "interactions": [], "exportedAt": "2026-01-01T00:00:00Z" }"""));

        var result = await facade.ExportAsync();

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Content.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportAsync_Unauthorized_ForcesSignOut()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.Unauthorized, """{ "title": "Unauthorized" }"""));

        var result = await facade.ExportAsync();

        result.IsSuccess.Should().BeFalse();
        result.RequireSignOut.Should().BeTrue();
    }

    // ── Request deletion ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RequestDataDeletionAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.OK, "{}"));

        var result = await facade.RequestDataDeletionAsync();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDataDeletionAsync_AlreadyPending_400_MapsToFriendlyMessage()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.BadRequest, """{ "title": "Gdpr.AlreadyPending" }"""));

        var result = await facade.RequestDataDeletionAsync();

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error!.ToLowerInvariant().Should().Contain("already scheduled");
    }

    [Fact]
    public async Task RequestDataDeletionAsync_Unauthorized_ForcesSignOut()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.Unauthorized, """{ "title": "Unauthorized" }"""));

        var result = await facade.RequestDataDeletionAsync();

        result.RequireSignOut.Should().BeTrue();
    }

    // ── Cancel deletion ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelDataDeletionAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.OK, "{}"));

        var result = await facade.CancelDataDeletionAsync();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CancelDataDeletionAsync_NotFound_404_MapsToNoPendingMessage()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.NotFound, """{ "title": "Gdpr.NotFound" }"""));

        var result = await facade.CancelDataDeletionAsync();

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error!.ToLowerInvariant().Should().Contain("no pending");
    }

    [Fact]
    public async Task CancelDataDeletionAsync_Unauthorized_ForcesSignOut()
    {
        var facade = CreateFacade(_ => Resp(HttpStatusCode.Unauthorized, """{ "title": "Unauthorized" }"""));

        var result = await facade.CancelDataDeletionAsync();

        result.RequireSignOut.Should().BeTrue();
    }
}
