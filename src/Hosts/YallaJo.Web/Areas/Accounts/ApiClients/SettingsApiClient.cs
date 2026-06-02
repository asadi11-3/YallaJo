using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class SettingsApiClient
{
    private readonly IApiClient _api;

    public SettingsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<NotificationPreferenceResponse>>> GetPreferencesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<NotificationPreferenceResponse>>("/api/v1/notifications/preferences", ct);

    public Task<ApiResult> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default)
        => _api.PutAsync("/api/v1/notifications/preferences", request, ct);

    public Task<ApiResult<MarketingConsentResponse>> GetMarketingConsentAsync(CancellationToken ct = default)
        => _api.GetAsync<MarketingConsentResponse>("/api/v1/accounts/me/marketing-consent", ct);

    public Task<ApiResult> UpdateMarketingConsentAsync(MarketingConsentRequest request, CancellationToken ct = default)
        => _api.PutAsync("/api/v1/accounts/me/marketing-consent", request, ct);
}
