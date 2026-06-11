using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
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
/// Host-level smoke for the CCD-5 article images flow. Boots the real YallaJo.Web host,
/// injects a signed-in user with a chosen permission set, and stubs IApiClient so
/// list/upload(PostFile)/delete/set-primary/reorder are deterministic without a live API.
///
/// Covers: save-draft-first (no BlogId); list/empty in editor; upload happy/validation/403;
/// delete happy/403; set-primary happy/403; reorder happy/403; permission gates; and that
/// the article text-save (PUT update) never calls an attachment endpoint.
/// </summary>
public sealed class CreatorArticleImagesFlowSmokeTests
{
    private const string Read = "Permission.Blog.Read";
    private const string Update = "Permission.Blog.Update";
    private const string AttRead = "Permission.Attachment.Read";
    private const string AttCreate = "Permission.Attachment.Create";
    private const string AttUpdate = "Permission.Attachment.Update";
    private const string AttDelete = "Permission.Attachment.Delete";
    private const string ImgUpdate = "Permission.EntityImage.Update";

    private static readonly Guid BlogId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid AttId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Editor_NewArticleHasNoImageTools_WhenNoBlogId()
    {
        // A freshly-loaded "new" editor (GET /new) has no BlogId → save-draft-first message.
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttCreate, "Permission.Blog.Create"]);

        var html = await (await client.GetAsync("/creator/articles/new")).Content.ReadAsStringAsync();
        html.Should().Contain("Save the draft first", "no image tools before a BlogId exists");
        html.Should().NotContain("name=\"files\"", "no upload control without a BlogId");
    }

    [Fact]
    public async Task Editor_ExistingArticle_ShowsImagesSection_WithList()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor(
            [Read, AttRead, AttCreate, AttDelete, AttUpdate, ImgUpdate],
            adminBlogJson: AdminBlogJson(), imagesJson: OneImageJson());

        var resp = await client.GetAsync($"/creator/articles/{BlogId}/edit");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Images");
        html.Should().Contain("https://cdn/a.jpg", "the existing image renders");
        html.Should().Contain("name=\"files\"", "upload control is available for an existing article");
    }

    [Fact]
    public async Task Editor_ExistingArticle_NoImages_ShowsEmptyState()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([Read, AttRead, AttCreate], adminBlogJson: AdminBlogJson(), imagesJson: "[]");

        var html = await (await client.GetAsync($"/creator/articles/{BlogId}/edit")).Content.ReadAsStringAsync();
        html.Should().Contain("No images yet");
    }

    [Fact]
    public async Task Upload_Happy_RedirectsToEdit()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttCreate], uploadStatus: 201);

        var resp = await PostUploadAsync(client, $"/creator/articles/{BlogId}/images/upload", existingCount: 0,
            ("a.jpg", "image/jpeg", "fakejpegbytes"));

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain($"/creator/articles/{BlogId}/edit");
        f.UploadCount.Should().Be(1, "the single valid image is uploaded");
    }

    [Fact]
    public async Task Upload_InvalidType_DoesNotCallApi()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttCreate]);

        var resp = await PostUploadAsync(client, $"/creator/articles/{BlogId}/images/upload", existingCount: 0,
            ("bad.svg", "image/svg+xml", "<svg/>"));

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        f.UploadCount.Should().Be(0, "an invalid file must be rejected client-side before any API call");
    }

    [Fact]
    public async Task Upload_Without_AttachmentCreate_Is403()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttRead]); // no Create

        var resp = await PostUploadAsync(client, $"/creator/articles/{BlogId}/images/upload", existingCount: 0,
            ("a.jpg", "image/jpeg", "bytes"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Happy_Redirects()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttDelete], writeStatus: 200);

        var resp = await PostFormAsync(client, $"/creator/articles/{BlogId}/images/{AttId}/delete", new());
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain($"/creator/articles/{BlogId}/edit");
    }

    [Fact]
    public async Task Delete_Without_AttachmentDelete_Is403()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttRead]);

        var resp = await PostFormAsync(client, $"/creator/articles/{BlogId}/images/{AttId}/delete", new());
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetPrimary_Happy_Redirects()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([ImgUpdate], writeStatus: 200);

        var resp = await PostFormAsync(client, $"/creator/articles/{BlogId}/images/{AttId}/primary", new());
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task SetPrimary_Without_EntityImageUpdate_Is403()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttCreate, AttDelete]); // no EntityImage.Update

        var resp = await PostFormAsync(client, $"/creator/articles/{BlogId}/images/{AttId}/primary", new());
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reorder_Happy_Redirects()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttUpdate], writeStatus: 200);

        var resp = await PostFormAsync(client, $"/creator/articles/{BlogId}/images/reorder",
            new() { ["orderedAttachmentIds"] = AttId.ToString() });
        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Reorder_Without_AttachmentUpdate_Is403()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([AttDelete]); // no Attachment.Update

        var resp = await PostFormAsync(client, $"/creator/articles/{BlogId}/images/reorder",
            new() { ["orderedAttachmentIds"] = AttId.ToString() });
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ArticleTextSave_DoesNotCallAttachmentEndpoints()
    {
        using var f = new ImagesFactory();
        var client = f.CreateClientFor([Update], writeStatus: 200);

        await PostFormAsync(client, $"/creator/articles/{BlogId}/edit",
            new()
            {
                ["Title"] = "Hello World", ["Content"] = "Body",
                ["SourceLanguageCode"] = "en", ["RowVersion"] = "AAAAAAAAB9E=",
            });

        f.UploadCount.Should().Be(0, "saving article text must never upload images");
        f.AttachmentApiHits.Should().Be(0, "article text-save must not touch attachment endpoints");
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────

    private static string AdminBlogJson() => $$"""
        { "id": "{{BlogId}}", "slug": "p", "title": "P", "content": "c", "status": "Draft",
          "languageCode": "en", "rowVersion": "AAAAAAAAB9E=", "isFeatured": false }
        """;

    private static string OneImageJson() => $$"""
        [ { "id": "{{AttId}}", "entityType": "Blog", "entityId": "{{BlogId}}", "type": "Image",
            "url": "https://cdn/a.jpg", "thumbnailUrl": null, "sortOrder": 0,
            "uploadedAt": "2026-01-01T00:00:00Z", "uploadedByUserId": "11111111-1111-1111-1111-111111111111" } ]
        """;

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, Dictionary<string, string> fields)
    {
        using var content = new FormUrlEncodedContent(fields);
        return await client.PostAsync(path, content);
    }

    private static async Task<HttpResponseMessage> PostUploadAsync(
        HttpClient client, string path, int existingCount,
        params (string Name, string ContentType, string Body)[] files)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(existingCount.ToString()), "existingCount" },
        };
        foreach (var (name, contentType, body) in files)
        {
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "files", name);
        }
        return await client.PostAsync(path, form);
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class ImagesFactory : WebApplicationFactory<Program>
    {
        public int UploadCount { get; internal set; }
        public int AttachmentApiHits { get; internal set; }

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
            string? adminBlogJson = null,
            string? imagesJson = null,
            int uploadStatus = 201,
            int writeStatus = 200)
        {
            UploadCount = 0;
            AttachmentApiHits = 0;
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(st => st.Permissions = permissions);
                    s.Configure<TestApiState>(st =>
                    {
                        st.AdminBlogJson = adminBlogJson;
                        st.ImagesJson = imagesJson;
                        st.UploadStatus = uploadStatus;
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

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }

    private sealed class TestApiState
    {
        public string? AdminBlogJson { get; set; }
        public string? ImagesJson { get; set; }
        public int UploadStatus { get; set; } = 201;
        public int WriteStatus { get; set; } = 200;
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "CreatorImagesSmokeScheme";
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
        private readonly ImagesFactory _factory;

        public StubApiClient(IOptions<TestApiState> state, ImagesFactory factory)
        {
            _state = state.Value;
            _factory = factory;
        }

        private bool IsAttachmentPath(string path) => path.Contains("/content-core/attachments", StringComparison.Ordinal);

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            string? json = null;
            if (path.StartsWith("/api/v1/blogs/admin/", StringComparison.Ordinal))
                json = _state.AdminBlogJson;
            else if (IsAttachmentPath(path))
            {
                _factory.AttachmentApiHits++;
                json = _state.ImagesJson;
            }

            if (string.IsNullOrWhiteSpace(json))
                return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));

            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null
                ? ApiResult<T>.Fail(200, "Could not parse response body.")
                : ApiResult<T>.Ok(data, 200));
        }

        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType,
            IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
        {
            _factory.AttachmentApiHits++;
            _factory.UploadCount++;
            if (_state.UploadStatus is >= 200 and < 300)
            {
                var json = $$"""{ "id": "{{Guid.NewGuid()}}", "url": "https://cdn/new.jpg", "fileSize": 100 }""";
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                    json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Task.FromResult(data is null
                    ? ApiResult<T>.Fail(_state.UploadStatus, "parse")
                    : ApiResult<T>.Ok(data, _state.UploadStatus));
            }
            return Task.FromResult(ApiResult<T>.Fail(_state.UploadStatus, "upload failed"));
        }

        public Task<ApiResult<T>> PostFilesAsync<T>(string path, IReadOnlyList<ApiUploadFile> files,
            IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "files", CancellationToken ct = default)
        {
            _factory.AttachmentApiHits++;
            _factory.UploadCount += files.Count;
            if (_state.UploadStatus is >= 200 and < 300)
            {
                var ids = string.Join(",", files.Select(_ => $"\"{Guid.NewGuid()}\""));
                var json = $$"""{ "uploadedAttachmentIds": [{{ids}}], "errors": [] }""";
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                    json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Task.FromResult(data is null
                    ? ApiResult<T>.Fail(_state.UploadStatus, "parse")
                    : ApiResult<T>.Ok(data, _state.UploadStatus));
            }
            return Task.FromResult(ApiResult<T>.Fail(_state.UploadStatus, "upload failed"));
        }

        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
        {
            if (IsAttachmentPath(path)) _factory.AttachmentApiHits++;
            return Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus) : ApiResult.Fail(_state.WriteStatus, null));
        }

        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
        {
            if (IsAttachmentPath(path)) _factory.AttachmentApiHits++;
            return Task.FromResult(_state.WriteStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.WriteStatus) : ApiResult.Fail(_state.WriteStatus, null));
        }

        // ── Unused by the image flow / article text-save ─────────────────────────
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
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
    }
}
