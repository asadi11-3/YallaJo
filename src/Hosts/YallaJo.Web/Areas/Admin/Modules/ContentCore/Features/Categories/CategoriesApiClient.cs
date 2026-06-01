using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories;

public sealed class CategoriesApiClient
{
    private readonly IApiClient _api;
    public CategoriesApiClient(IApiClient api) => _api = api;

    // Admin list (all categories including inactive) — ?isActive=false
    public Task<ApiResult<List<CategoryItemResponse>>> GetCategoriesAsync(
        bool includeInactive, CancellationToken ct = default)
        => _api.GetAsync<List<CategoryItemResponse>>(
            $"/api/v1/content-core/categories?isActive={(includeInactive ? "false" : "true")}", ct);

    public Task<ApiResult<CategoryItemResponse>> GetAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<CategoryItemResponse>($"/api/v1/content-core/categories/{id}", ct);

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
