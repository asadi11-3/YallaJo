using YallaJo.Web.Areas.Admin.Models.BlogTranslations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// Typed access to the admin Blog translation endpoints (Phase 5A backend).
/// Knows endpoint URLs only; all calls go through <see cref="IApiClient"/>.
/// </summary>
public sealed class BlogTranslationsApiClient
{
    private readonly IApiClient _api;

    public BlogTranslationsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/blogs/admin/{id}/translations
    public Task<ApiResult<List<BlogTranslationResponse>>> GetTranslationsAsync(
        Guid blogId, CancellationToken ct = default)
        => _api.GetAsync<List<BlogTranslationResponse>>(
            $"/api/v1/blogs/admin/{blogId}/translations", ct);

    // GET /api/v1/blogs/admin/{id}/translations/{languageCode}
    public Task<ApiResult<BlogTranslationResponse>> GetTranslationAsync(
        Guid blogId, string languageCode, CancellationToken ct = default)
        => _api.GetAsync<BlogTranslationResponse>(
            $"/api/v1/blogs/admin/{blogId}/translations/{Uri.EscapeDataString(languageCode)}", ct);

    // PUT /api/v1/blogs/admin/{id}/translations/{languageCode}
    public Task<ApiResult> UpsertTranslationAsync(
        Guid blogId, string languageCode, UpsertBlogTranslationRequest request, CancellationToken ct = default)
        => _api.PutAsync(
            $"/api/v1/blogs/admin/{blogId}/translations/{Uri.EscapeDataString(languageCode)}", request, ct);
}
