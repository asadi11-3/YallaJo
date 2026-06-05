using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.EntityTags;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class EntityTagsApiClient
{
    private const string Base = "/api/v1/content-core/entity-tags";

    private readonly IApiClient _api;

    public EntityTagsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<EntityTagItemResponse>>> GetAssignedAsync(
        string entityType, Guid entityId, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["entityType"] = entityType,
            ["entityId"] = entityId.ToString()
        });
        return _api.GetAsync<List<EntityTagItemResponse>>(url, ct);
    }

    public Task<ApiResult> AssignAsync(AssignTagsToEntityRequest request, CancellationToken ct = default)
        => _api.PostAsync(Base, request, ct);

    public Task<ApiResult> RemoveAsync(string entityType, Guid entityId, Guid tagId, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["entityType"] = entityType,
            ["entityId"] = entityId.ToString(),
            ["tagId"] = tagId.ToString()
        });
        return _api.DeleteAsync(url, ct);
    }
}
