using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Areas.Auth.ApiClients;

namespace YallaJo.Web.Areas.Auth.Facades;
public sealed class DevicesFacade
{
    private readonly DevicesApiClient _api;
    private readonly IWebSignInService _signIn;

    public DevicesFacade(DevicesApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<ApiResult> TrustAsync(Guid deviceId, CancellationToken ct = default)
    {
        var result = await _api.TrustDeviceAsync(deviceId, ct);

        if (result.IsSuccess) return ApiResult.Ok();

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        return ApiResult.Fail(result.Error ?? "Trust failed.");
    }
}
