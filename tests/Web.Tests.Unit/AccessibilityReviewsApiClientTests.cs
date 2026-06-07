using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2D-1 — the accessibility-reviews ApiClient targets ONLY
/// /api/v1/social/accessibility/reviews* (never /api/v1/social/reviews and never the
/// /places/.../accessibility feature endpoints), with the correct verbs/bodies. DELETE is
/// bodyless and no request carries a RowVersion.
/// </summary>
public sealed class AccessibilityReviewsApiClientTests
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
            return new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private static (AccessibilityReviewsApiClient Sut, CapturingHandler Handler) CreateSut(
        HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var handler = new CapturingHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new AccessibilityReviewsApiClient(api), handler);
    }

    [Fact]
    public async Task GetForEntityAsync_TargetsAccessibilityEndpoint_WithQuery()
    {
        var (sut, handler) = CreateSut(body: """{"items":[],"page":1,"pageSize":10,"totalCount":0}""");
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await sut.GetForEntityAsync("Place", id, 1, 10);

        var uri = handler.Calls[0].RequestUri!;
        uri.AbsolutePath.Should().Be("/api/v1/social/accessibility/reviews");
        uri.AbsolutePath.Should().NotContain("/social/reviews/Place");   // not normal reviews
        uri.Query.Should().Contain("entityType=Place");
        uri.Query.Should().Contain($"entityId={id}");
        uri.Query.Should().Contain("page=1");
        uri.Query.Should().Contain("pageSize=10");
    }

    [Fact]
    public async Task GetMyAsync_TargetsMyEndpoint()
    {
        var (sut, handler) = CreateSut(body: """{"items":[],"nextCursor":null}""");
        await sut.GetMyAsync(cursor: null, pageSize: 20);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/social/accessibility/reviews/my");
        handler.Calls[0].RequestUri!.Query.Should().Contain("pageSize=20");
    }

    [Fact]
    public async Task CreateAsync_PostsBody_WithFeatureTypesCsv_NoRowVersion()
    {
        var (sut, handler) = CreateSut();
        var body = new CreateAccessibilityReviewBody(
            AccessibilityReviewTargetType.Place,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            4.5m, "Step-free", "Great ramp access and wide doors.", null, "Wheelchair,Mobility");

        await sut.CreateAsync(body);

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/social/accessibility/reviews");
        handler.Bodies[0].Should().Contain("Wheelchair,Mobility");
        handler.Bodies[0].Should().NotContain("RowVersion", "accessibility reviews never send a RowVersion");
        handler.Bodies[0].Should().NotContain("rowVersion");
    }

    [Fact]
    public async Task EditAsync_PutsToIdRoute_NoRowVersion()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        await sut.EditAsync(id, new UpdateAccessibilityReviewBody(5m, null, "Updated content here.", null, "Visual"));

        handler.Calls[0].Method.Should().Be(HttpMethod.Put);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/social/accessibility/reviews/{id}");
        handler.Bodies[0].Should().NotContain("RowVersion");
    }

    [Fact]
    public async Task DeleteAsync_IsBodyless()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await sut.DeleteAsync(id);

        handler.Calls[0].Method.Should().Be(HttpMethod.Delete);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/social/accessibility/reviews/{id}");
        handler.Bodies[0].Should().BeNull("DELETE must be bodyless (no RowVersion)");
    }
}
