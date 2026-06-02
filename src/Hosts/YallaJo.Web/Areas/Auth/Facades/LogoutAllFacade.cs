using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Areas.Auth.ApiClients;

namespace YallaJo.Web.Areas.Auth.Facades;
public sealed class LogoutAllFacade
{
    private readonly LogoutAllApiClient _api;
    private readonly IWebSignInService  _signIn;

    public LogoutAllFacade(LogoutAllApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<ApiResult> HandleAsync(CancellationToken ct = default)
    {
        var result = await _api.LogoutAllAsync(ct);

        // Always clear the local auth cookie — the user intends to sign out of every session.
        await _signIn.SignOutAsync();

        return result;
    }
}
