using YallaJo.Web.Areas.Auth.Features.Sessions.Mappers;
using YallaJo.Web.Areas.Auth.Features.Sessions.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.Sessions;

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
}
