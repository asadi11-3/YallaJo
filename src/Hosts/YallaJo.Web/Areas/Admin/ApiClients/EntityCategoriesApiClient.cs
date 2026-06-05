using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.EntityCategories;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class EntityCategoriesApiClient
{
    private const string Base = "/api/v1/content-core/entity-categories";
    private readonly IApiClient _api;

    public EntityCategoriesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<EntityCategoryItemResponse>>> GetAssignedAsync(
        string entityType, Guid entityId, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["entityType"] = entityType,
            ["entityId"] = entityId.ToString()
        });
        return _api.GetAsync<List<EntityCategoryItemResponse>>(url, ct);
    }

    public Task<ApiResult> AssignAsync(AssignCategoriesToEntityRequest request, CancellationToken ct = default)
        => _api.PostAsync(Base, request, ct);

    public Task<ApiResult> RemoveAsync(string entityType, Guid entityId, Guid categoryId, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["entityType"] = entityType,
            ["entityId"] = entityId.ToString(),
            ["categoryId"] = categoryId.ToString()
        });
        return _api.DeleteAsync(url, ct);
    }
}
