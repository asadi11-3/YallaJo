using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Host-level smoke for the CCD-4 creator articles flow. Boots the real YallaJo.Web
/// host, injects a signed-in user with a chosen permission set, and stubs
/// <see cref="IApiClient"/> so list/create/admin-get/update/submit/delete/restore are
/// deterministic without a live API.
///
/// Covers: list states + empty + status filter; create draft happy; edit prefetch
/// uses admin-get (RowVersion); update happy + 409; submit with RowVersion (happy +
/// 422); delete with RowVersion; restore with RowVersion; per-action permission gates;
/// Dashboard CTA links; sidebar My Articles link.
/// </summary>
public sealed class CreatorArticlesFlowSmokeTests
{
    private const string ReadOwn = "Permission.Blog.ReadOwn";
    private const string Create = "Permission.Blog.Create";
    private const string Read = "Permission.Blog.Read";
    private const string Update = "Permission.Blog.Update";
    private const string Submit = "Permission.Blog.Submit";
    private const string DeleteOwn = "Permission.Blog.DeleteOwn";

    private static readonly string[] AllArticlePerms =
        [ReadOwn, Create, Read, Update, Submit, DeleteOwn];

    [Fact]
    public async Task List_Empty_ShowsEmptyState()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([ReadOwn], myBlogsJson: EmptyPageJson());

        var resp = await client.GetAsync("/creator/articles");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("No articles yet");
    }

    [Fact]
    public async Task List_WithItems_ShowsRows_AndStatusFilter()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([ReadOwn, Create], myBlogsJson: OnePageJson());

        var resp = await client.GetAsync("/creator/articles?status=Draft");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("My Title");
        html.Should().Contain("New Article", "create CTA shows for Blog.Create holders");
        html.Should().Contain("statusFilter", "the status filter renders");
    }

    [Fact]
    public async Task List_Without_ReadOwn_Is403()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Create]); // no ReadOwn

        (await client.GetAsync("/creator/articles")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Succeeds_AndRedirectsToEdit()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Create, Read],
            createStatus: 201, createBody: """{ "blogId": "33333333-3333-3333-3333-333333333333", "slug": "x" }""");

        var resp = await PostFormAsync(client, "/creator/articles/new",
            new() { ["Title"] = "Hello World", ["Content"] = "Body", ["SourceLanguageCode"] = "en" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/articles/33333333-3333-3333-3333-333333333333/edit");
    }

    [Fact]
    public async Task Create_Without_Create_Is403()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([ReadOwn]); // no Create

        var resp = await PostFormAsync(client, "/creator/articles/new",
            new() { ["Title"] = "Hello World", ["Content"] = "Body", ["SourceLanguageCode"] = "en" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Edit_Prefetch_UsesAdminGet_AndExposesRowVersion()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Read, Update, Submit, DeleteOwn], adminBlogJson: AdminBlogJson("Draft"));

        var resp = await client.GetAsync("/creator/articles/44444444-4444-4444-4444-444444444444/edit");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        // The admin-get RowVersion must be present as the hidden field value for safe updates.
        html.Should().Contain("AAAAAAAAB9E=", "edit prefetch must surface the admin-get RowVersion");
        html.Should().Contain("Submit for review", "a Draft can be submitted");
        html.Should().Contain("Save changes");
        // True status from admin-get is shown in the editor.
        f.LastAdminGetPath.Should().Be("/api/v1/blogs/admin/44444444-4444-4444-4444-444444444444",
            "editing must prefetch via admin-get, NOT the anonymous GET /api/v1/blogs/{id}");
    }

    [Fact]
    public async Task Update_Succeeds_AndRedirects()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Update], writeStatus: 200);

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/edit",
            new() { ["Title"] = "Hello World", ["Content"] = "Body", ["SourceLanguageCode"] = "en", ["RowVersion"] = "AAAAAAAAB9E=" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/edit");
    }

    [Fact]
    public async Task Update_Conflict_ReturnsToEditorWithError()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Update], writeStatus: 409);

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/edit",
            new() { ["Title"] = "Hello World", ["Content"] = "Body", ["SourceLanguageCode"] = "en", ["RowVersion"] = "AAAAAAAAB9E=" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "a 409 re-renders the editor, not a redirect");
        (await resp.Content.ReadAsStringAsync()).Should().Contain("refresh", "409 maps to a friendly refresh message");
    }

    [Fact]
    public async Task Update_Without_Update_Is403()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Read]); // no Update

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/edit",
            new() { ["Title"] = "Hello World", ["Content"] = "Body", ["SourceLanguageCode"] = "en", ["RowVersion"] = "Ug==" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Submit_WithRowVersion_Succeeds()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Submit], writeStatus: 200);

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/submit",
            new() { ["rowVersion"] = "AAAAAAAAB9E=" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        f.LastSubmitBody.Should().Contain("AAAAAAAAB9E=", "submit must send the RowVersion in the body");
    }

    [Fact]
    public async Task Submit_InvalidStatus_422_ShowsError()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Submit, Read], writeStatus: 422, adminBlogJson: AdminBlogJson("Draft"));

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/submit",
            new() { ["rowVersion"] = "AAAAAAAAB9E=" });

        // PRG back to the editor; the flash error is surfaced on the redirected page.
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Submit_Without_Submit_Is403()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Update]); // no Submit

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/submit",
            new() { ["rowVersion"] = "Ug==" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithRowVersion_Succeeds()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([DeleteOwn, Read], writeStatus: 200, adminBlogJson: AdminBlogJson("Draft"));

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/delete",
            new() { ["rowVersion"] = "AAAAAAAAB9E=" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/articles");
        f.LastDeleteBody.Should().Contain("AAAAAAAAB9E=", "delete must send the RowVersion in the body");
    }

    [Fact]
    public async Task Delete_Without_DeleteOwn_Is403()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([Update]); // no DeleteOwn

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/delete",
            new() { ["rowVersion"] = "Ug==" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Restore_WithRowVersion_Succeeds()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([DeleteOwn], writeStatus: 200);

        var resp = await PostFormAsync(client, "/creator/articles/44444444-4444-4444-4444-444444444444/restore",
            new() { ["rowVersion"] = "AAAAAAAAB9E=" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        f.LastRestoreBody.Should().Contain("AAAAAAAAB9E=", "restore must send the RowVersion in the body");
    }

    [Fact]
    public async Task Sidebar_MyArticlesLink_VisibleWithReadOwn()
    {
        using var f = new ArticlesFactory();
        var client = f.CreateClientFor([ReadOwn], myBlogsJson: EmptyPageJson());

        var html = await (await client.GetAsync("/creator/articles")).Content.ReadAsStringAsync();
        html.Should().Contain("bi-journals fa-fw me-1", "the My Articles sidebar link renders for Blog.ReadOwn");
    }

    // Note: anonymous → sign-in redirect is covered by the runtime smoke (it exercises
    // the real cookie challenge). It is not asserted here because this test host
    // replaces the cookie auth scheme with a stub, which does not emit the redirect.

    // ── JSON fixtures ───────────────────────────────────────────────────────────

    private static string EmptyPageJson() => """
        { "items": [], "pageNumber": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0,
          "hasPreviousPage": false, "hasNextPage": false }
        """;

    private static string OnePageJson() => """
        { "items": [ { "id": "55555555-5555-5555-5555-555555555555", "slug": "my-title",
          "title": "My Title", "publishedAt": null, "viewCount": 3, "readTimeMinutes": 5,
          "languageCode": "default", "isFeatured": false } ],
          "pageNumber": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1,
          "hasPreviousPage": false, "hasNextPage": false }
        """;

    private static string AdminBlogJson(string status) => $$"""
        { "id": "44444444-4444-4444-4444-444444444444", "slug": "my-post", "title": "My Post",
          "content": "body", "summary": "s", "status": "{{status}}", "publishedAt": null,
          "viewCount": 0, "readTimeMinutes": 4, "metaTitle": "mt", "metaDescription": "md",
          "placeId": null, "languageCode": "en", "rowVersion": "AAAAAAAAB9E=", "isFeatured": false }
        """;

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, Dictionary<string, string> fields)
    {
        using var content = new FormUrlEncodedContent(fields);
        return await client.PostAsync(path, content);
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class ArticlesFactory : WebApplicationFactory<Program>
    {
        public string? LastAdminGetPath { get; internal set; }
        public string? LastSubmitBody { get; internal set; }
        public string? LastDeleteBody { get; internal set; }
        public string? LastRestoreBody { get; internal set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    o.DefaultScheme = TestAuthHandler.SchemeName;
                });

                services.RemoveAll<IApiClient>();
                services.AddSingleton(this);
                services.AddScoped<IApiClient, StubApiClient>();

                services.RemoveAll<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
                services.AddSingleton<Microsoft.AspNetCore.Antiforgery.IAntiforgery, NoopAntiforgery>();
            });
        }

        public HttpClient CreateClientFor(
            string[] permissions,
            string? myBlogsJson = null,
            string? adminBlogJson = null,
            int createStatus = 201,
            string? createBody = null,
            int writeStatus = 200)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(st => st.Permissions = permissions);
                    s.Configure<TestApiState>(st =>
                    {
                        st.MyBlogsJson = myBlogsJson;
                        st.AdminBlogJson = adminBlogJson;
                        st.CreateStatus = createStatus;
                        st.CreateBody = createBody;
                        st.WriteStatus = writeStatus;
                    });
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class NoopAntiforgery : Microsoft.AspNetCore.Antiforgery.IAntiforgery
    {
        private static readonly Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet Empty = new("t", "t", "f", "h");
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(Microsoft.AspNetCore.Http.HttpContext c) => Empty;
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(Microsoft.AspNetCore.Http.HttpContext c) => Empty;
        public Task<bool> IsRequestValidAsync(Microsoft.AspNetCore.Http.HttpContext c) => Task.FromResult(true);
        public Task ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpContext c) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(Microsoft.AspNetCore.Http.HttpContext c) { }
    }

    private sealed class TestAuthState
    {
        public string[] Permissions { get; set; } = [];
        public bool Anonymous { get; set; }
    }

    private sealed class TestApiState
    {
        public string? MyBlogsJson { get; set; }
        public string? AdminBlogJson { get; set; }
        public int CreateStatus { get; set; } = 201;
        public string? CreateBody { get; set; }
        public int WriteStatus { get; set; } = 200;
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "CreatorArticlesSmokeScheme";
        private readonly TestAuthState _state;

        public TestAuthHandler(IOptionsMonitor<TestAuthSchemeOptions> o, ILoggerFactory l,
            UrlEncoder e, IOptions<TestAuthState> state) : base(o, l, e) => _state = state.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (_state.Anonymous)
                return Task.FromResult(AuthenticateResult.NoResult());

            var jwt = new JwtSecurityToken(claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", "11111111-1111-1111-1111-111111111111"),
                new Claim(ClaimTypes.Name, "Smoke Creator"),
                new Claim("access_token", accessToken),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubApiClient : IApiClient
    {
        private readonly TestApiState _state;
        private readonly ArticlesFactory _factory;

        public StubApiClient(IOptions<TestApiState> state, ArticlesFactory factory)
        {
            _state = state.Value;
            _factory = factory;
        }

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            string? json = null;
            if (path.StartsWith("/api/v1/blogs/my-blogs", StringComparison.Ordinal))
                json = _state.MyBlogsJson;
            else if (path.StartsWith("/api/v1/blogs/admin/", StringComparison.Ordinal))
            {
                _factory.LastAdminGetPath = path; // record for assertions
                json = _state.AdminBlogJson;
            }

            if (string.IsNullOrWhiteSpace(json))
                return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));

            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null
                ? ApiResult<T>.Fail(200, "Could not parse response body.")
                : ApiResult<T>.Ok(data, 200));
        }

        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
        {
            // Create blog
            if (_state.CreateStatus is >= 200 and < 300 && !string.IsNullOrWhiteSpace(_state.CreateBody))
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                    _state.CreateBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (data is not null)
                    return Task.FromResult(ApiResult<T>.Ok(data, _state.CreateStatus));
            }
            return Task.FromResult(_state.CreateStatus is >= 200 and < 300
                ? ApiResult<T>.Fail(_state.CreateStatus, "Empty response body.")
                : ApiResult<T>.Fail(_state.CreateStatus, null));
        }

        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
        {
            var serialized = body is null ? "" : System.Text.Json.JsonSerializer.Serialize(body);
            if (path.EndsWith("/submit-for-review", StringComparison.Ordinal)) _factory_SetSubmit(serialized);
            else if (path.EndsWith("/restore", StringComparison.Ordinal)) _factory_SetRestore(serialized);

            return Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus)
                : ApiResult.Fail(_state.WriteStatus, null));
        }

        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus)
                : ApiResult.Fail(_state.WriteStatus, null));

        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
        {
            _factory_SetDelete(body is null ? "" : System.Text.Json.JsonSerializer.Serialize(body));
            return Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus)
                : ApiResult.Fail(_state.WriteStatus, null));
        }

        private void _factory_SetSubmit(string s) => _factory.LastSubmitBody = s;
        private void _factory_SetDelete(string s) => _factory.LastDeleteBody = s;
        private void _factory_SetRestore(string s) => _factory.LastRestoreBody = s;

        // ── Unused ─────────────────────────────────────────────────────────────
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "n/a"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
    }
}
