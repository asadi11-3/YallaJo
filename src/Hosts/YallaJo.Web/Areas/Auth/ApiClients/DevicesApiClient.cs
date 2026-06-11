using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.ApiClients;
public sealed class DevicesApiClient
{
    private readonly IApiClient _api;
    public DevicesApiClient(IApiClient api) => _api = api;

    // PATCH /api/v1/auth/devices/{id}/trust — mark a session device as trusted.
    public Task<ApiResult> TrustDeviceAsync(Guid deviceId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/auth/devices/{deviceId}/trust", null, ct);

    // DELETE /api/v1/auth/devices/{id}/trust — remove the trusted mark from a device.
    public Task<ApiResult> UntrustDeviceAsync(Guid deviceId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/auth/devices/{deviceId}/trust", ct);

    // ── Push-notification device tokens (Messaging /devices/*) — §3.10 Devices ──

    // GET /api/v1/devices/tokens — the current user's registered device tokens.
    public Task<ApiResult<List<DeviceTokenResponse>>> GetTokensAsync(CancellationToken ct = default)
        => _api.GetAsync<List<DeviceTokenResponse>>("/api/v1/devices/tokens", ct);

    // POST /api/v1/devices/token — register a device token for push.
    public Task<ApiResult<Guid>> RegisterTokenAsync(RegisterDeviceTokenApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<Guid>("/api/v1/devices/token", request, ct);

    // DELETE /api/v1/devices/token/{id} — revoke a registered device token.
    public Task<ApiResult> DeleteTokenAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/devices/token/{id}", ct);
}
