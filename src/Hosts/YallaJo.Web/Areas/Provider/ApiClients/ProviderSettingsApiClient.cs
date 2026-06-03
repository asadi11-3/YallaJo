using YallaJo.Web.Areas.Provider.Models.Settings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderSettingsApiClient
{
    private readonly IApiClient _api;

    public ProviderSettingsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ProviderSettingsResponse>> GetSettingsAsync(CancellationToken ct = default)
        => _api.GetAsync<ProviderSettingsResponse>("/api/v1/provider/settings", ct);
}
