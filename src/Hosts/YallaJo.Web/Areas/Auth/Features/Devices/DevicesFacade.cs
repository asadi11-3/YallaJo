using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.Devices;

public sealed class DevicesFacade
{
    private readonly DevicesApiClient _api;
    private readonly IWebSignInService _signIn;

    public DevicesFacade(DevicesApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<DevicesFacadeResult> TrustAsync(Guid deviceId, CancellationToken ct = default)
    {
        var result = await _api.TrustDeviceAsync(deviceId, ct);

        if (result.IsSuccess) return DevicesFacadeResult.Ok();

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return DevicesFacadeResult.ForceSignOut();
        }

        return DevicesFacadeResult.Fail(result.Error ?? "Trust failed.");
    }
}

public sealed class DevicesFacadeResult
{
    public bool    IsSuccess      { get; private init; }
    public string? Error          { get; private init; }
    public bool    RequireSignOut { get; private init; }

    public static DevicesFacadeResult Ok()           => new() { IsSuccess = true };
    public static DevicesFacadeResult Fail(string e) => new() { IsSuccess = false, Error = e };
    public static DevicesFacadeResult ForceSignOut() => new() { IsSuccess = false, RequireSignOut = true };
}
