using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2D-1 — mapper + facade behavior: CSV parsing, privacy-safe author handle, the 48h
/// edit-window flag, and friendly mapping of 409 (duplicate) / edit-window / 403 / success.
/// </summary>
public sealed class AccessibilityReviewsFacadeTests
{
    // ── Mapper ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseFeatures_SplitsCsv_AndTrims()
        => AccessibilityReviewMapper.ParseFeatures("Wheelchair, Mobility ,Visual")
            .Should().BeEquivalentTo("Wheelchair", "Mobility", "Visual");

    [Fact]
    public void ToCsv_KeepsOnlyKnownKinds_AndDedupes()
        => AccessibilityReviewMapper.ToCsv(["Wheelchair", "wheelchair", "Bogus", "Visual"])
            .Should().Be("Wheelchair,Visual");

    [Fact]
    public void AuthorHandle_IsPrivacySafe_NoRawGuid()
        => AccessibilityReviewMapper.AuthorHandle(Guid.Parse("abcdef00-0000-0000-0000-000000000000"))
            .Should().Be("Reviewer abcdef");

    [Fact]
    public void Row_IsWithinEditWindow_TrueWhenRecent_FalseWhenOld()
    {
        var recent = AccessibilityReviewMapper.ToRow(MakeResponse(DateTime.UtcNow.AddHours(-1)));
        var old = AccessibilityReviewMapper.ToRow(MakeResponse(DateTime.UtcNow.AddHours(-49)));
        recent.IsWithinEditWindow.Should().BeTrue();
        old.IsWithinEditWindow.Should().BeFalse();
    }

    private static AccessibilityReviewResponse MakeResponse(DateTime createdAt) => new(
        Guid.NewGuid(), Guid.NewGuid(), AccessibilityReviewTargetType.Place, Guid.NewGuid(),
        4m, "t", "content over ten chars", null, "Wheelchair",
        AccessibilityReviewStatus.Visible, createdAt, null);

    // ── Facade mapping ──────────────────────────────────────────────────────────

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(status)
            { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
    }

    private static AccessibilityReviewsFacade CreateFacade(HttpStatusCode status, string body = "{}")
    {
        var http = new HttpClient(new StubHandler(status, body)) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return new AccessibilityReviewsFacade(new AccessibilityReviewsApiClient(api));
    }

    private static AccessibilityReviewFormVm Form() => new()
    {
        TargetType = "Place",
        TargetId = Guid.NewGuid(),
        Rating = 4m,
        Content = "Accessible entrance and restrooms.",
        FeatureTypes = ["Wheelchair"],
    };

    [Fact]
    public async Task SubmitAsync_Duplicate409_MapsFriendly()
    {
        var facade = CreateFacade(HttpStatusCode.Conflict, """{"title":"AccessibilityReview.Duplicate"}""");
        var result = await facade.SubmitAsync(Form());
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already submitted");
    }

    [Fact]
    public async Task EditAsync_EditWindow_MapsFriendly()
    {
        var facade = CreateFacade(HttpStatusCode.BadRequest,
            """{"title":"AccessibilityReview.EditWindowExpired","detail":"only be edited within 48 hours"}""");
        var result = await facade.EditAsync(Guid.NewGuid(), Form());
        result.Error.Should().Contain("48 hours");
    }

    [Fact]
    public async Task EditAsync_Forbidden_MapsOwnership()
    {
        var facade = CreateFacade(HttpStatusCode.Forbidden, """{"title":"AccessibilityReview.OwnerMismatch"}""");
        var result = await facade.EditAsync(Guid.NewGuid(), Form());
        result.Error.Should().Contain("your own accessibility review");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_MapsFriendly()
    {
        var facade = CreateFacade(HttpStatusCode.NotFound, """{"title":"AccessibilityReview.NotFound"}""");
        var result = await facade.DeleteAsync(Guid.NewGuid());
        result.Error.Should().Contain("could not be found");
    }

    [Fact]
    public async Task SubmitAsync_Unauthorized_ForcesSignOut()
    {
        var facade = CreateFacade(HttpStatusCode.Unauthorized, "{}");
        (await facade.SubmitAsync(Form())).RequireSignOut.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_Success()
    {
        var facade = CreateFacade(HttpStatusCode.OK, "\"" + Guid.NewGuid() + "\"");
        (await facade.SubmitAsync(Form())).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetListAsync_MapsItems_FromPublicPage()
    {
        var facade = CreateFacade(HttpStatusCode.OK, """
            {"items":[{"id":"11111111-1111-1111-1111-111111111111","userId":"22222222-2222-2222-2222-222222222222","targetType":1,"targetId":"33333333-3333-3333-3333-333333333333","rating":4.5,"title":"t","content":"Accessible entrance.","visitDate":null,"featureTypesCsv":"Wheelchair,Visual","status":0,"createdAt":"2026-02-01T00:00:00Z","lastEditedAt":null}],"page":1,"pageSize":10,"totalCount":1}
            """);

        var vm = await facade.GetListAsync("Place", Guid.NewGuid());

        vm.HasResults.Should().BeTrue();
        vm.TotalCount.Should().Be(1);
        vm.Items[0].FeatureTypes.Should().BeEquivalentTo("Wheelchair", "Visual");
        vm.Items[0].AuthorHandle.Should().StartWith("Reviewer ");
    }

    [Fact]
    public async Task GetListAsync_OnFailure_ReturnsEmpty_NotThrow()
    {
        var facade = CreateFacade(HttpStatusCode.InternalServerError, "boom");
        var vm = await facade.GetListAsync("Place", Guid.NewGuid());
        vm.HasResults.Should().BeFalse();
    }
}
