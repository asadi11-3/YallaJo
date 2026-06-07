using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.AgencyRoster;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2C-1 — verifies the agency-roster ApiClient targets ONLY the Accounts agency
/// endpoints (/api/v1/agency/*), never the ContentTours tour endpoints (/api/v1/tours/*),
/// with the correct verbs and bodies — including the DELETE-with-body for remove.
/// </summary>
public sealed class AgencyRosterApiClientTests
{
    private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
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

    private static (AgencyRosterApiClient Sut, CapturingHandler Handler) CreateSut(
        HttpStatusCode status = HttpStatusCode.OK, string body = "[]")
    {
        var handler = new CapturingHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new AgencyRosterApiClient(api), handler);
    }

    [Fact]
    public async Task GetGuidesAsync_TargetsAgencyGuides()
    {
        var (sut, handler) = CreateSut();
        await sut.GetGuidesAsync();
        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/agency/guides");
    }

    [Fact]
    public async Task GetApplicationsAsync_TargetsAgencyApplications_NotTours()
    {
        var (sut, handler) = CreateSut();
        await sut.GetApplicationsAsync();
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/agency/applications");
        handler.Calls[0].RequestUri!.AbsolutePath.Should().NotContain("/tours/");
    }

    [Fact]
    public async Task GetSentInvitationsAsync_TargetsSentInvitations()
    {
        var (sut, handler) = CreateSut();
        await sut.GetSentInvitationsAsync();
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/agency/invitations/sent");
    }

    [Fact]
    public async Task GetAvailableGuidesAsync_IncludesPaging()
    {
        var (sut, handler) = CreateSut();
        await sut.GetAvailableGuidesAsync(page: 2, pageSize: 25);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/agency/guides/available");
        handler.Calls[0].RequestUri!.Query.Should().Contain("page=2");
        handler.Calls[0].RequestUri!.Query.Should().Contain("pageSize=25");
    }

    [Fact]
    public async Task InviteGuideAsync_PostsInviteBody()
    {
        var (sut, handler) = CreateSut();
        var guideId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await sut.InviteGuideAsync(new InviteGuideApiRequest(guideId, "join us", 15m));
        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/agency/guides/invite");
        handler.Bodies[0].Should().Contain(guideId.ToString());
        handler.Bodies[0].Should().Contain("15");
    }

    [Fact]
    public async Task ApproveApplicationAsync_PostsToApprove_WithNullBody()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await sut.ApproveApplicationAsync(id);
        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/agency/applications/{id}/approve");
        handler.Bodies[0].Should().BeNull();
    }

    [Fact]
    public async Task RejectApplicationAsync_PostsReason()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        await sut.RejectApplicationAsync(id, new RejectAgencyApplicationApiRequest("not a fit"));
        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/agency/applications/{id}/reject");
        handler.Bodies[0].Should().Contain("not a fit");
    }

    [Fact]
    public async Task RemoveGuideAsync_SendsDeleteWithBody()
    {
        var (sut, handler) = CreateSut();
        var guideId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await sut.RemoveGuideAsync(guideId, new RemoveAgencyGuideApiRequest("left agency"));
        handler.Calls[0].Method.Should().Be(HttpMethod.Delete);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/agency/guides/{guideId}");
        handler.Bodies[0].Should().NotBeNull();
        handler.Bodies[0].Should().Contain("left agency");
    }
}
