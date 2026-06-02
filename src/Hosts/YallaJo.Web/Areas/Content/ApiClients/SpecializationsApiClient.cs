using YallaJo.Web.Areas.Content.Models.Guides;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.ApiClients;

public sealed class SpecializationsApiClient
{
    private readonly IApiClient _api;

    public SpecializationsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<SpecializationResponse>>> ListSpecializationsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<SpecializationResponse>>("/api/v1/content-core/specializations", ct);
}
