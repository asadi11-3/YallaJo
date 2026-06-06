using YallaJo.Web.Areas.Admin.Models.Categories;
using YallaJo.Web.Areas.Admin.Models.Categories;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class CategoriesApiClient
{
    private readonly IApiClient _api;
    public CategoriesApiClient(IApiClient api) => _api = api;

    // Admin list. Uses the permission-gated admin route, which (unlike the public
    // anonymous route) honours the activeOnly flag and can return inactive categories.
    // activeOnly defaults to false server-side, so includeInactive=true sends nothing;
    // includeInactive=false explicitly requests active-only.
    public Task<ApiResult<List<CategoryItemResponse>>> GetCategoriesAsync(
        bool includeInactive, CancellationToken ct = default)
        => _api.GetAsync<List<CategoryItemResponse>>(
            includeInactive
                ? "/api/v1/content-core/categories/admin"
                : "/api/v1/content-core/categories/admin?activeOnly=true", ct);

    // Admin detail. Uses the admin route, which can return inactive categories
    // (includeInactive defaults to true server-side).
    public Task<ApiResult<CategoryItemResponse>> GetAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<CategoryItemResponse>($"/api/v1/content-core/categories/admin/{id}", ct);

    public Task<ApiResult> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/content-core/categories", request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/content-core/categories/{id}", request, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/content-core/categories/{id}", ct);

    public Task<ApiResult> DeactivateAsync(Guid id, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/content-core/categories/{id}/deactivate", null, ct);

    public Task<ApiResult> ActivateAsync(Guid id, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/content-core/categories/{id}/activate", null, ct);
}
