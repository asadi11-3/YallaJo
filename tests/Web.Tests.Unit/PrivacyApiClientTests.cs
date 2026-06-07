using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Services;
using WebPermission = YallaJo.Web.Infrastructure.Authorization.WebPermission;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1D — verifies <see cref="PrivacyApiClient"/> hits the correct HTTP verb + URL for
/// each GDPR data-right endpoint (export / request-deletion / cancel-deletion) on the
/// Analytics recommendations group.
/// </summary>
public sealed class PrivacyApiClientTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _contentType;
        public List<HttpRequestMessage> Calls { get; } = new();

        public CapturingHandler(string contentType = "application/json") => _contentType = contentType;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, _contentType),
            };
            return Task.FromResult(resp);
        }
    }

    private static (PrivacyApiClient Sut, CapturingHandler Handler) CreateSut(string contentType = "application/json")
    {
        var handler = new CapturingHandler(contentType);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new PrivacyApiClient(api), handler);
    }

    [Fact]
    public async Task ExportAsync_Gets_MeExportRoute()
    {
        var (sut, handler) = CreateSut();

        var result = await sut.ExportAsync();

        result.IsSuccess.Should().BeTrue();
        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/analytics/recommendations/me/export");
    }

    [Fact]
    public async Task RequestDataDeletionAsync_Deletes_MeRoute()
    {
        var (sut, handler) = CreateSut();

        await sut.RequestDataDeletionAsync();

        handler.Calls[0].Method.Should().Be(HttpMethod.Delete);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/analytics/recommendations/me");
    }

    [Fact]
    public async Task CancelDataDeletionAsync_Posts_CancelDeletionRoute()
    {
        var (sut, handler) = CreateSut();

        await sut.CancelDataDeletionAsync();

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/analytics/recommendations/me/cancel-deletion");
    }

    [Fact]
    public void Preference_permission_constants_match_backend_format()
    {
        WebPermission.Preference.Read.Should().Be("Permission.Preference.Read");
        WebPermission.Preference.Update.Should().Be("Permission.Preference.Update");
    }
}
