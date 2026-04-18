using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations;

public sealed class SpecializationsApiClient
{
    private readonly ApiClient _api;
    public SpecializationsApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<List<SpecializationItemResponse>>> GetSpecializationsAsync(
        bool activeOnly, CancellationToken ct = default)
        => _api.GetAsync<List<SpecializationItemResponse>>(
            $"/api/v1/content-core/specializations?activeOnly={(activeOnly ? "true" : "false")}", ct);

    public Task<ApiResult> CreateAsync(CreateSpecializationRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/content-core/specializations", request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateSpecializationRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/content-core/specializations/{id}", request, ct);
}
