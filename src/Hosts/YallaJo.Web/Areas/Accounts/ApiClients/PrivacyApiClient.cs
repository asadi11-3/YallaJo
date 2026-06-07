using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

/// <summary>
/// FE-1D — thin HTTP client for the current user's GDPR data rights over their
/// personal analytics/recommendation data (Analytics module, group base
/// /api/v1/analytics/recommendations). These endpoints derive the caller from the
/// JWT, so there is no id in the route and the client is inherently self-scoped.
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'ApiClient' suffix.
/// </summary>
public sealed class PrivacyApiClient
{
    private const string Base = "/api/v1/analytics/recommendations";

    private readonly IApiClient _api;

    public PrivacyApiClient(IApiClient api) => _api = api;

    /// <summary>
    /// GDPR data-portability export. Proxies the raw JSON bytes through the BFF
    /// (via <see cref="IApiClient.GetFileAsync"/>) so the payload is re-served to the
    /// browser as a file download without lossy re-serialization and without the JWT
    /// ever leaving the server. Requires Permission.Preference.Read.
    /// </summary>
    public Task<ApiResult<ApiFile>> ExportAsync(CancellationToken ct = default)
        => _api.GetFileAsync($"{Base}/me/export", ct);

    /// <summary>
    /// Requests deletion of all personal analytics data (30-day window before hard
    /// delete). Returns 400 (Gdpr.AlreadyPending) when a request is already pending.
    /// Requires Permission.Preference.Update.
    /// </summary>
    public Task<ApiResult> RequestDataDeletionAsync(CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/me", ct);

    /// <summary>
    /// Cancels a pending analytics-data deletion request. Returns 404 (Gdpr.NotFound)
    /// when no request is pending. Requires Permission.Preference.Update.
    /// </summary>
    public Task<ApiResult> CancelDataDeletionAsync(CancellationToken ct = default)
        => _api.PostAsync($"{Base}/me/cancel-deletion", null, ct);
}
