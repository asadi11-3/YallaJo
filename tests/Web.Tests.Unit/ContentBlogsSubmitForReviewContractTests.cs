using FluentAssertions;
using YallaJo.Web.Areas.Content.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Articles;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Creator Backend Contract Polish (Gap 5): the legacy Content composer's
/// SubmitForReviewAsync must send the article's RowVersion (BlogRowVersionRequestBody),
/// not a null body — the backend /submit-for-review endpoint requires it.
/// </summary>
public sealed class ContentBlogsSubmitForReviewContractTests
{
    [Fact]
    public async Task SubmitForReviewAsync_PostsRowVersionBody_NotNull()
    {
        var api = new CapturingApiClient();
        var client = new BlogsApiClient(api);
        var id = Guid.NewGuid();

        await client.SubmitForReviewAsync(id, "AAAAAAAAB9E=");

        api.LastPath.Should().Be($"/api/v1/blogs/{id}/submit-for-review");
        api.LastBody.Should().NotBeNull("the backend requires a RowVersion body (Gap 5 fix)");
        api.LastBody.Should().BeOfType<BlogRowVersionRequestBody>();
        ((BlogRowVersionRequestBody)api.LastBody!).RowVersion.Should().Be("AAAAAAAAB9E=");
    }

    [Fact]
    public async Task ListMyBlogsAsync_RequestsPaginatedMyBlogsEndpoint()
    {
        var api = new CapturingApiClient();
        var client = new BlogsApiClient(api);

        await client.ListMyBlogsAsync(page: 2, pageSize: 10, status: "Draft");

        api.LastGetPath.Should().Contain("/api/v1/blogs/my-blogs");
        api.LastGetPath.Should().Contain("page=2");
        api.LastGetPath.Should().Contain("pageSize=10");
        api.LastGetPath.Should().Contain("status=Draft");
    }

    // Minimal capturing IApiClient — records the outbound path/body of the call under test.
    private sealed class CapturingApiClient : IApiClient
    {
        public string? LastPath { get; private set; }
        public string? LastGetPath { get; private set; }
        public object? LastBody { get; private set; }

        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
        {
            LastPath = path;
            LastBody = body;
            return Task.FromResult(ApiResult.Ok(200));
        }

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            LastGetPath = path;
            return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        }

        // ── Unused ─────────────────────────────────────────────────────────────
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
    }
}
