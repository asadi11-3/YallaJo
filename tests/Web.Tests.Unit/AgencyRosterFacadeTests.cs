using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.AgencyRoster;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2C-1 — verifies the roster facade aggregates the three read endpoints into a
/// single VM, computes pending counts, maps status badges, and surfaces sign-out.
/// </summary>
public sealed class AgencyRosterFacadeTests
{
    // Returns canned JSON based on the request path so the three reads resolve.
    private sealed class RouteHandler(Func<string, (HttpStatusCode, string)> map) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (status, body) = map(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static GuideAgencyRosterFacade CreateFacade(Func<string, (HttpStatusCode, string)> map)
    {
        var http = new HttpClient(new RouteHandler(map)) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return new GuideAgencyRosterFacade(new AgencyRosterApiClient(api), NullLogger<GuideAgencyRosterFacade>.Instance);
    }

    [Fact]
    public async Task GetRosterAsync_AggregatesAllThreeSections_AndCountsPending()
    {
        var facade = CreateFacade(path => path switch
        {
            "/api/v1/agency/guides" => (HttpStatusCode.OK, """
                [{"affiliationId":"a1111111-1111-1111-1111-111111111111","guideUserId":"b1111111-1111-1111-1111-111111111111","commissionPercentage":20,"joinedAt":"2026-01-01T00:00:00Z"}]
                """),
            "/api/v1/agency/applications" => (HttpStatusCode.OK, """
                [{"id":"c1111111-1111-1111-1111-111111111111","guideUserId":"d1111111-1111-1111-1111-111111111111","agencyUserId":"e1111111-1111-1111-1111-111111111111","message":"hi","status":0,"rejectionReason":null,"reviewedAt":null,"createdAt":"2026-01-02T00:00:00Z"}]
                """),
            "/api/v1/agency/invitations/sent" => (HttpStatusCode.OK, """
                [{"id":"f1111111-1111-1111-1111-111111111111","agencyUserId":"e1111111-1111-1111-1111-111111111111","guideUserId":"01111111-1111-1111-1111-111111111111","message":null,"proposedCommissionPercentage":15,"status":0,"expiresAt":"2026-02-01T00:00:00Z","respondedAt":null,"createdAt":"2026-01-03T00:00:00Z"}]
                """),
            _ => (HttpStatusCode.OK, "[]"),
        });

        var result = await facade.GetRosterAsync();

        result.IsSuccess.Should().BeTrue();
        var vm = result.Data!;
        vm.HasGuides.Should().BeTrue();
        vm.ActiveGuideCount.Should().Be(1);
        vm.HasApplications.Should().BeTrue();
        vm.PendingApplicationCount.Should().Be(1);
        vm.HasSentInvitations.Should().BeTrue();
        vm.PendingInvitationCount.Should().Be(1);
        vm.Guides[0].GuideHandle.Should().StartWith("Guide ");
    }

    [Fact]
    public async Task GetRosterAsync_EmptyEverywhere_ReturnsEmptyVm()
    {
        var facade = CreateFacade(_ => (HttpStatusCode.OK, "[]"));

        var result = await facade.GetRosterAsync();

        result.IsSuccess.Should().BeTrue();
        result.Data!.HasGuides.Should().BeFalse();
        result.Data.HasApplications.Should().BeFalse();
        result.Data.HasSentInvitations.Should().BeFalse();
    }

    [Fact]
    public async Task GetRosterAsync_OnUnauthorized_ForcesSignOut()
    {
        var facade = CreateFacade(_ => (HttpStatusCode.Unauthorized, "{}"));

        var result = await facade.GetRosterAsync();

        result.RequireSignOut.Should().BeTrue();
    }

    [Fact]
    public async Task GetRosterAsync_OnForbidden_Fails403()
    {
        var facade = CreateFacade(_ => (HttpStatusCode.Forbidden, """{"title":"Forbidden"}"""));

        var result = await facade.GetRosterAsync();

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Theory]
    [InlineData(AgencyApplicationStatus.Pending, "bg-warning text-dark")]
    [InlineData(AgencyApplicationStatus.Approved, "bg-success")]
    [InlineData(AgencyApplicationStatus.Rejected, "bg-danger")]
    public void ApplicationBadge_MapsStatus(AgencyApplicationStatus status, string expected)
        => AgencyRosterMapper.ApplicationBadge(status).Should().Be(expected);

    [Fact]
    public void GuideHandle_IsPrivacySafe_NoRawGuid()
    {
        var handle = AgencyRosterMapper.GuideHandle(Guid.Parse("b0000000-0000-0000-0000-000000000005"));
        handle.Should().Be("Guide b0000000");
    }
}
