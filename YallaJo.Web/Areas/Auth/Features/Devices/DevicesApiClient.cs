using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.Devices;

public sealed class DevicesApiClient
{
    private readonly ApiClient _api;
    public DevicesApiClient(ApiClient api) => _api = api;

    /// <summary>PATCH /api/v1/auth/devices/{deviceId}/trust</summary>
    public Task<ApiResult> TrustDeviceAsync(Guid deviceId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/auth/devices/{deviceId}/trust", null, ct);
}
