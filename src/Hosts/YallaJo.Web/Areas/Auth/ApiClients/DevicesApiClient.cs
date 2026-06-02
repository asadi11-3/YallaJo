using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.ApiClients;
public sealed class DevicesApiClient
{
    private readonly IApiClient _api;
    public DevicesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult> TrustDeviceAsync(Guid deviceId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/auth/devices/{deviceId}/trust", null, ct);
}
