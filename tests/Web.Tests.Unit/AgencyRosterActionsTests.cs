using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2C-3 — approve / reject / remove: the facade forwards to the agency endpoints,
/// sends the reason body, and maps 403/404/409/422 to friendly messages.
/// </summary>
public sealed class AgencyRosterActionsTests
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

    private static (GuideAgencyRosterFacade Facade, StubHandler Handler) Create(HttpStatusCode status, string body = "{}")
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new GuideAgencyRosterFacade(new AgencyRosterApiClient(api), NullLogger<GuideAgencyRosterFacade>.Instance), handler);
    }

    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task ApproveAsync_Success()
    {
        var (facade, _) = Create(HttpStatusCode.OK);
        (await facade.ApproveApplicationAsync(Id)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAsync_Conflict_MapsFriendly()
    {
        var (facade, _) = Create(HttpStatusCode.Conflict, """{"title":"Agency.ApplicationNotPending"}""");
        var result = await facade.ApproveApplicationAsync(Id);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no longer pending");
    }

    [Fact]
    public async Task ApproveAsync_Forbidden_MapsFriendly()
    {
        var (facade, _) = Create(HttpStatusCode.Forbidden, """{"title":"Agency.NotOwner"}""");
        var result = await facade.ApproveApplicationAsync(Id);
        result.Error.Should().Contain("your own agency");
    }

    [Fact]
    public async Task RejectAsync_SendsReason()
    {
        var (facade, handler) = Create(HttpStatusCode.OK);
        await facade.RejectApplicationAsync(Id, "not enough experience");
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/agency/applications/{Id}/reject");
        handler.Bodies[0].Should().Contain("not enough experience");
    }

    [Fact]
    public async Task RejectAsync_NotFound_MapsFriendly()
    {
        var (facade, _) = Create(HttpStatusCode.NotFound, """{"title":"Agency.ApplicationNotFound"}""");
        var result = await facade.RejectApplicationAsync(Id, "x");
        result.Error.Should().Contain("could not be found");
    }

    [Fact]
    public async Task RemoveGuideAsync_SendsDeleteWithReasonBody()
    {
        var (facade, handler) = Create(HttpStatusCode.OK);
        await facade.RemoveGuideAsync(Id, "left agency");
        handler.Calls[0].Method.Should().Be(HttpMethod.Delete);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/agency/guides/{Id}");
        handler.Bodies[0].Should().NotBeNull();
        handler.Bodies[0].Should().Contain("left agency");
    }

    [Fact]
    public async Task RemoveGuideAsync_Forbidden_MapsFriendly()
    {
        var (facade, _) = Create(HttpStatusCode.Forbidden, """{"title":"Agency.NotOwner"}""");
        var result = await facade.RemoveGuideAsync(Id, "x");
        result.Error.Should().Contain("your own agency");
    }

    [Fact]
    public async Task RemoveGuideAsync_NotFound_MapsFriendly()
    {
        var (facade, _) = Create(HttpStatusCode.NotFound, """{"title":"Agency.NotAffiliated"}""");
        var result = await facade.RemoveGuideAsync(Id, "x");
        result.Error.Should().Contain("not currently on your roster");
    }

    [Fact]
    public async Task RemoveGuideAsync_Conflict_MapsReload()
    {
        var (facade, _) = Create(HttpStatusCode.Conflict, """{"title":"AgencyAffiliation.ConcurrencyConflict"}""");
        var result = await facade.RemoveGuideAsync(Id, "x");
        result.Error.Should().Contain("Reload");
    }
}
