using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages;

public sealed class LanguagesApiClient
{
    private readonly IApiClient _api;
    public LanguagesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<LanguageItemResponse>>> GetLanguagesAsync(
        bool activeOnly, CancellationToken ct = default)
        => _api.GetAsync<List<LanguageItemResponse>>(
            $"/api/v1/content-core/languages?activeOnly={(activeOnly ? "true" : "false")}", ct);

    public Task<ApiResult> CreateAsync(
        CreateLanguageRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/content-core/languages", request, ct);

    public Task<ApiResult> UpdateAsync(
        Guid id, UpdateLanguageRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/content-core/languages/{id}", request, ct);
}
