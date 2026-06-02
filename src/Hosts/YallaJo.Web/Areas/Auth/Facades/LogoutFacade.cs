using YallaJo.Web.Areas.Auth.Models.Logout;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Areas.Auth.ApiClients;

namespace YallaJo.Web.Areas.Auth.Facades;
public sealed class LogoutFacade
{
    private readonly LogoutApiClient   _api;
    private readonly IWebSignInService _signIn;

    public LogoutFacade(LogoutApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<ApiResult> HandleAsync(string? refreshToken, CancellationToken ct = default)
    {
        var result = ApiResult.Ok();

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            result = await _api.LogoutAsync(new LogoutRequest { RefreshToken = refreshToken }, ct);
        }

        // Always clear the local auth cookie, even if the server-side revoke failed —
        // the user intends to sign out and must not stay logged in locally.
        await _signIn.SignOutAsync();

        return result;
    }
}
