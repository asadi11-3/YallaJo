using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-0B (BR-5) — verifies <see cref="ToursApiClient.GetTourBySlugAsync"/> targets
/// the canonical <c>/api/v1/tours/by-slug/{slug}</c> route rather than the legacy
/// <c>/api/v1/tours/slug/{slug}</c> alias. The URL is the contract between Web and
/// the ContentTours tour endpoints; both backend routes remain alive, but the FE
/// consumer must use the canonical one.
/// </summary>
public sealed class PublicToursApiClientUrlTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static (ToursApiClient Sut, CapturingHandler Handler) CreateSut()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new ToursApiClient(api), handler);
    }

    [Fact]
    public async Task GetTourBySlugAsync_ShouldCall_Canonical_BySlugRoute()
    {
        var (sut, handler) = CreateSut();

        var result = await sut.GetTourBySlugAsync("petra-day-tour");

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().HaveCount(1);
        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/tours/by-slug/petra-day-tour");
    }

    [Fact]
    public async Task GetTourBySlugAsync_ShouldNotUse_LegacySlugRoute()
    {
        var (sut, handler) = CreateSut();

        await sut.GetTourBySlugAsync("petra-day-tour");

        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().NotBe("/api/v1/tours/slug/petra-day-tour");
        // The legacy alias is "/tours/slug/"; the canonical "/tours/by-slug/" must not
        // be mistaken for it. Assert the legacy segment is absent.
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().NotContain("/tours/slug/");
    }

    [Fact]
    public async Task GetTourBySlugAsync_ShouldUrlEscape_TheSlug()
    {
        var (sut, handler) = CreateSut();

        await sut.GetTourBySlugAsync("dead sea & spa");

        // Uri.EscapeDataString encodes space as %20 and '&' as %26; the canonical
        // base path must still be present.
        handler.Calls[0].RequestUri!.AbsoluteUri
            .Should().Contain("/api/v1/tours/by-slug/dead%20sea%20%26%20spa");
    }
}
