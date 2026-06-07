using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.AgencyRoster;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2C-2 — invite flow: the facade posts the invite body, maps backend failures
/// (404/409/422) to friendly messages, and builds the invite form from available guides.
/// </summary>
public sealed class AgencyRosterInviteTests
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

    private static GuideAgencyRosterFacade CreateFacade(HttpStatusCode status, string body = "{}")
    {
        var http = new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return new GuideAgencyRosterFacade(new AgencyRosterApiClient(api), NullLogger<GuideAgencyRosterFacade>.Instance);
    }

    private static InviteGuideFormVm Form() => new()
    {
        GuideUserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        ProposedCommissionPercentage = 18m,
        Message = "  join us  ",
    };

    [Fact]
    public async Task InviteAsync_Success_ReturnsSuccess()
    {
        var facade = CreateFacade(HttpStatusCode.Created, "\"22222222-2222-2222-2222-222222222222\"");
        var result = await facade.InviteAsync(Form());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InviteAsync_NotFound_MapsFriendly()
    {
        var facade = CreateFacade(HttpStatusCode.NotFound, """{"title":"Agency.GuideNotFound"}""");
        var result = await facade.InviteAsync(Form());
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("could not be found");
    }

    [Fact]
    public async Task InviteAsync_Conflict_MapsFriendly()
    {
        var facade = CreateFacade(HttpStatusCode.Conflict, """{"title":"Agency.AlreadyAffiliated"}""");
        var result = await facade.InviteAsync(Form());
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already");
    }

    [Fact]
    public async Task InviteAsync_ValidationError_SurfacesMessage()
    {
        var facade = CreateFacade(HttpStatusCode.UnprocessableEntity,
            """{"title":"Validation","detail":"Proposed commission percentage must be between 0 and 100."}""");
        var result = await facade.InviteAsync(Form());
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task GetInviteFormAsync_PopulatesAvailableGuides()
    {
        var facade = CreateFacade(HttpStatusCode.OK, """
            [{"userId":"33333333-3333-3333-3333-333333333333","businessName":"Petra Tours","contactEmail":"p@x.test","createdAt":"2026-01-01T00:00:00Z"}]
            """);

        var result = await facade.GetInviteFormAsync();

        result.IsSuccess.Should().BeTrue();
        result.Data!.HasAvailableGuides.Should().BeTrue();
        result.Data.AvailableGuides[0].BusinessName.Should().Be("Petra Tours");
    }

    [Fact]
    public void InviteForm_DefaultsCommissionTo20()
        => new InviteGuideFormVm().ProposedCommissionPercentage.Should().Be(20m);
}
