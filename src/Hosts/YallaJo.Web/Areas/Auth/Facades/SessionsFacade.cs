using YallaJo.Web.Areas.Auth.Models.Sessions;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Areas.Auth.ApiClients;

namespace YallaJo.Web.Areas.Auth.Facades;
public sealed class SessionsFacade
{
    private readonly SessionsApiClient _api;
    private readonly IWebSignInService _signIn;

    public SessionsFacade(SessionsApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<ApiResult<SessionsVm>> GetSessionsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetSessionsAsync(ct);

        if (result.IsSuccess)
        {
            var vm = new SessionsVm
            {
                Sessions = (result.Data ?? []).Select(SessionsMapper.ToVm).ToList()
            };
            return ApiResult<SessionsVm>.Ok(vm);
        }

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult<SessionsVm>.ForceSignOut();
        }

        return ApiResult<SessionsVm>.Fail(0, result.Error ?? "Could not load sessions.");
    }

    public async Task<ApiResult> RevokeAsync(Guid sessionId, CancellationToken ct = default)
    {
        var result = await _api.RevokeSessionAsync(sessionId, ct);

        if (result.IsSuccess || result.IsNotFound)
            return ApiResult.Ok();

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        return ApiResult.Fail(result.Error ?? "Revoke failed.");
    }

    /// <summary>Revokes every session except the caller's current one. Returns the revoked count.</summary>
    public async Task<ApiResult<int>> RevokeOthersAsync(CancellationToken ct = default)
    {
        var result = await _api.RevokeOthersAsync(ct);

        if (result.IsSuccess)
            return ApiResult<int>.Ok(result.Data?.RevokedCount ?? 0);

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult<int>.ForceSignOut();
        }

        return ApiResult<int>.Fail(0, result.Error ?? "Revoke failed.");
    }
}
