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

public sealed class CreatorProfileFlowSmokeTests
{
    private const string Read = "Permission.Creator.Read";
    private const string Update = "Permission.Creator.Update";
    private const string Delete = "Permission.Creator.Delete";

    [Fact]
    public async Task Get_NoProfile_RedirectsToApplication()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read]); // profile read returns "absent"

        var resp = await client.GetAsync("/creator/profile");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/application");
    }

    [Fact]
    public async Task Get_ActiveProfile_RendersEditableForm()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update, Delete], profileJson: ActiveJson());

        var resp = await client.GetAsync("/creator/profile");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Save profile", "an Active profile is editable");
        html.Should().Contain("Deactivate", "the danger zone is shown for an Active profile");
        // Cover image support was removed end-to-end (no product feature).
        html.Should().NotContain("Cover image", "cover image support must be fully removed");
        html.Should().NotContain("/creator/profile/cover", "the cover route must be gone");
    }

    [Fact]
    public async Task Get_SuspendedProfile_RendersReadOnlyNotice()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read], profileJson: ActiveJson().Replace("\"Active\"", "\"Suspended\""));

        var resp = await client.GetAsync("/creator/profile");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("suspended");
        html.Should().NotContain("Save profile");
    }

    [Fact]
    public async Task Update_Succeeds_AndRedirects()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update], writeStatus: 200);

        var resp = await PostFormAsync(client, "/creator/profile",
            new() { ["DisplayName"] = "Jane" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/profile");
    }

    [Fact]
    public async Task Update_SlugConflict_ReturnsToFormWithInlineError()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update], writeStatus: 409);

        var resp = await PostFormAsync(client, "/creator/profile",
            new() { ["DisplayName"] = "Jane" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "a 409 re-renders the form, not a redirect");
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("slug is already taken", "409 maps to a friendly inline message");
    }

    [Fact]
    public async Task Update_Without_CreatorUpdate_Is403()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read]); // no Update

        var resp = await PostFormAsync(client, "/creator/profile",
            new() { ["DisplayName"] = "Jane" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AvatarUpload_Succeeds_AndRedirects_ViaManagedEndpoint()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update], writeStatus: 200);

        var resp = await PostAvatarFileAsync(client, ValidPng(), "avatar.png", "image/png");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/profile");
        // CA-2: the managed upload endpoint must have been called (NOT a ContentCore attachment).
        f.LastPostFilePath.Should().Be("/api/v1/blogs/creators/profile/mine/avatar/upload");
    }

    [Fact]
    public async Task AvatarUpload_Failure_ReturnsToFormWithError()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update], profileJson: ActiveJson(), writeStatus: 422);

        var resp = await PostAvatarFileAsync(client, ValidPng(), "avatar.png", "image/png");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/profile");
        f.LastPostFilePath.Should().Be("/api/v1/blogs/creators/profile/mine/avatar/upload");
    }

    [Fact]
    public async Task RemoveAvatar_Succeeds_AndRedirects_ViaClearEndpoint()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update], writeStatus: 200);

        var resp = await PostFormAsync(client, "/creator/profile/avatar/remove", new());

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/profile");
        // CA-2: the clear endpoint (DELETE) must have been called.
        f.LastDeletePath.Should().Be("/api/v1/blogs/creators/profile/mine/avatar");
    }

    [Fact]
    public async Task RemoveAvatar_Without_CreatorUpdate_Is403()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read]); // no Update

        var resp = await PostFormAsync(client, "/creator/profile/avatar/remove", new());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden, "RemoveAvatar is gated by Creator.Update");
    }

    [Fact]
    public async Task Deactivate_Confirmed_Succeeds()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Delete], writeStatus: 200);

        var resp = await PostFormAsync(client, "/creator/profile/deactivate",
            new() { ["confirm"] = "true" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/profile");
    }

    [Fact]
    public async Task Deactivate_Without_CreatorDelete_Is403()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update]); // has Update but NOT Delete

        var resp = await PostFormAsync(client, "/creator/profile/deactivate",
            new() { ["confirm"] = "true" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Deactivate is gated by Creator.Delete");
    }

    [Fact]
    public async Task Sidebar_MyProfileLink_VisibleWithCreatorRead()
    {
        using var f = new ProfileFactory();
        var client = f.CreateClientFor([Read, Update], profileJson: ActiveJson());

        var resp = await client.GetAsync("/creator/profile");
        var html = await resp.Content.ReadAsStringAsync();

        html.Should().Contain("bi-person-badge fa-fw me-1", "the My Profile sidebar link renders for Creator.Read");
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────

    private static string ActiveJson() => """
        {
          "id": "22222222-2222-2222-2222-222222222222",
          "userId": "11111111-1111-1111-1111-111111111111",
          "slug": "jane-creator",
          "displayName": "Jane Creator",
          "bio": "hello",
          "avatarUrl": "https://cdn/a.jpg",
          "trustTier": "Trusted",
          "status": "Active",
          "articleCount": 3,
          "followerCount": 12
        }
        """;

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, Dictionary<string, string> fields)
    {
        using var content = new FormUrlEncodedContent(fields);
        return await client.PostAsync(path, content);
    }

    private static async Task<HttpResponseMessage> PostAvatarFileAsync(
        HttpClient client, byte[] bytes, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "avatarFile", fileName);
        return await client.PostAsync("/creator/profile/avatar/upload", content);
    }

    // Minimal valid PNG signature (8-byte magic + a few bytes); content is not validated by the Web layer.
    private static byte[] ValidPng() =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class ProfileFactory : WebApplicationFactory<Program>
    {
        private readonly CallLog _callLog = new();

        public string? LastPostFilePath => _callLog.LastPostFilePath;
        public string? LastDeletePath => _callLog.LastDeletePath;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
            builder.UseFakeTestSecrets(); // Patch 0A.2: boot host without ambient user-secrets

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

                services.AddSingleton(_callLog);
                services.RemoveAll<IApiClient>();
                services.AddScoped<IApiClient, StubApiClient>();

                services.RemoveAll<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
                services.AddSingleton<Microsoft.AspNetCore.Antiforgery.IAntiforgery, NoopAntiforgery>();
            });
        }

        public HttpClient CreateClientFor(string[] permissions, string? profileJson = null, int writeStatus = 200)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(st => st.Permissions = permissions);
                    s.Configure<TestApiState>(st =>
                    {
                        st.ProfileMineJson = profileJson;
                        st.WriteStatus = writeStatus;
                    });
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }).WithEnglishCulture();
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

    private sealed class CallLog
    {
        public string? LastPostFilePath { get; set; }
        public string? LastDeletePath { get; set; }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }

    private sealed class TestApiState
    {
        public string? ProfileMineJson { get; set; }
        public int WriteStatus { get; set; } = 200;
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "CreatorProfileSmokeScheme";
        private readonly TestAuthState _state;

        public TestAuthHandler(IOptionsMonitor<TestAuthSchemeOptions> o, ILoggerFactory l,
            UrlEncoder e, IOptions<TestAuthState> state) : base(o, l, e) => _state = state.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwt = new JwtSecurityToken(claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", "11111111-1111-1111-1111-111111111111"),
                new Claim(ClaimTypes.Name, "Jane Creator"),
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
        private readonly CallLog _callLog;
        public StubApiClient(IOptions<TestApiState> state, CallLog callLog)
        {
            _state = state.Value;
            _callLog = callLog;
        }

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (path == "/api/v1/blogs/creators/profile/mine" && !string.IsNullOrWhiteSpace(_state.ProfileMineJson))
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                    _state.ProfileMineJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (data is not null)
                    return Task.FromResult(ApiResult<T>.Ok(data, 200));
            }
            // Absent: mirror real ApiClient's 200-empty-body failure.
            return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        }

        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus)
                : ApiResult.Fail(_state.WriteStatus, null));

        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
        {
            _callLog.LastDeletePath = path;
            return Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus)
                : ApiResult.Fail(_state.WriteStatus, null));
        }

        public Task<ApiResult> PostFileAsync(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
        {
            _callLog.LastPostFilePath = path;
            return Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus)
                : ApiResult.Fail(_state.WriteStatus, null));
        }

        // ── Unused ─────────────────────────────────────────────────────────────
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
    }
}
