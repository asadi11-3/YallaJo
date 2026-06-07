using YallaJo.Web.Areas.Accounts.Models.Settings;
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

    // ── Push-notification device tokens (§3.10 Devices tab) ──────────────────────

    /// <summary>
    /// Lists the user's registered device tokens. Safe-degrades: returns an empty list
    /// (never throws / blocks the Settings page) on any non-success.
    /// </summary>
    public async Task<IReadOnlyList<DeviceTokenRowVm>> GetTokensAsync(CancellationToken ct = default)
    {
        var result = await _api.GetTokensAsync(ct);
        if (!result.IsSuccess || result.Data is null) return [];

        return result.Data
            .OrderByDescending(d => d.LastSeenAt)
            .Select(d => new DeviceTokenRowVm(d.Id, d.DeviceId, d.Platform, d.LastSeenAt, d.CreatedAt))
            .ToList();
    }

    public async Task<ApiResult> RegisterTokenAsync(
        string deviceId, string platform, string token, CancellationToken ct = default)
    {
        var result = await _api.RegisterTokenAsync(
            new RegisterDeviceTokenApiRequest(deviceId.Trim(), platform.Trim(), token.Trim()), ct);

        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsConflict) return ApiResult.Fail(409, result.Error ?? "This device is already registered.");
        if (result.IsValidationError && result.ValidationErrors is { Count: > 0 })
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not register the device.");
    }

    public async Task<ApiResult> DeleteTokenAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.DeleteTokenAsync(id, ct);

        if (result.IsSuccess || result.IsNotFound) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not remove the device.");
    }
}
