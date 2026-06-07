using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AdminCategoriesApiClient = YallaJo.Web.Areas.Admin.ApiClients.CategoriesApiClient;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-0A (BR-1) — verifies the Admin <c>CategoriesApiClient</c> targets the
/// permission-gated admin routes (<c>/categories/admin</c> and
/// <c>/categories/admin/{id}</c>) so the Admin UI can see inactive/deactivated
/// categories. The URLs are the contract between Web and the ContentCore
/// category endpoints; any drift silently breaks the admin list.
/// </summary>
public sealed class AdminCategoriesApiClientTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _jsonBody;

        public CapturingHandler(string jsonBody = "[]") => _jsonBody = jsonBody;

        public List<HttpRequestMessage> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_jsonBody, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static (AdminCategoriesApiClient Sut, CapturingHandler Handler) CreateAdminSut(string jsonBody = "[]")
    {
        var handler = new CapturingHandler(jsonBody);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new AdminCategoriesApiClient(api), handler);
    }

    [Fact]
    public async Task GetCategoriesAsync_IncludeInactive_ShouldCall_AdminList_WithoutActiveOnlyFilter()
    {
        var (sut, handler) = CreateAdminSut();

        var result = await sut.GetCategoriesAsync(includeInactive: true);

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().HaveCount(1);
        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/content-core/categories/admin");
        // includeInactive=true must NOT force activeOnly=true (would hide inactive rows).
        handler.Calls[0].RequestUri!.Query.Should().NotContain("activeOnly=true");
        // Must not use the legacy/ignored public-route param.
        handler.Calls[0].RequestUri!.Query.Should().NotContain("isActive");
    }

    [Fact]
    public async Task GetCategoriesAsync_ActiveOnly_ShouldCall_AdminList_WithActiveOnlyTrue()
    {
        var (sut, handler) = CreateAdminSut();

        await sut.GetCategoriesAsync(includeInactive: false);

        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/content-core/categories/admin");
        handler.Calls[0].RequestUri!.Query.Should().Contain("activeOnly=true");
    }

    [Fact]
    public async Task GetAsync_ShouldCall_AdminDetail_ForGivenId()
    {
        var (sut, handler) = CreateAdminSut("{}");
        var id = Guid.Parse("99999999-9999-9999-9999-999999999999");

        await sut.GetAsync(id);

        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/content-core/categories/admin/99999999-9999-9999-9999-999999999999");
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldSurface_InactiveCategory_InResult()
    {
        // The admin route returns inactive categories; the client must not filter them out.
        const string body = """
        [
          { "id": "11111111-1111-1111-1111-111111111111", "name": "Active Cat",   "slug": "active-cat",   "isActive": true,  "sortOrder": 1 },
          { "id": "22222222-2222-2222-2222-222222222222", "name": "Inactive Cat", "slug": "inactive-cat", "isActive": false, "sortOrder": 2 }
        ]
        """;
        var (sut, _) = CreateAdminSut(body);

        var result = await sut.GetCategoriesAsync(includeInactive: true);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().Contain(c => !c.IsActive && c.Name == "Inactive Cat",
            "the admin list must include deactivated categories so admins can manage them");
    }

}
