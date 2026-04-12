using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.LogoutAll;

public sealed class LogoutAllFacade
{
    private readonly LogoutAllApiClient _api;
    private readonly IWebSignInService  _signIn;

    public LogoutAllFacade(LogoutAllApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    /// <summary>
    /// Revokes all sessions on the backend then clears the local cookie.
    /// Always signs out locally — even on backend failure.
    /// </summary>
    public async Task HandleAsync(CancellationToken ct = default)
    {
        await _api.LogoutAllAsync(ct); // best-effort
        await _signIn.SignOutAsync();
    }
}
