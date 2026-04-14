using System.Security.Claims;
using YallaJo.Web.Areas.Auth.Features.Logout.Requests;
using YallaJo.Web.Infrastructure.Authentication.Claims;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.Logout;

public sealed class LogoutFacade
{
    private readonly LogoutApiClient   _api;
    private readonly IWebSignInService _signIn;
    private readonly IHttpContextAccessor _accessor;

    public LogoutFacade(LogoutApiClient api, IWebSignInService signIn, IHttpContextAccessor accessor)
    {
        _api      = api;
        _signIn   = signIn;
        _accessor = accessor;
    }

    public async Task HandleAsync(CancellationToken ct = default)
    {
        var refreshToken = _accessor.HttpContext?.User
            .FindFirstValue(AppClaimTypes.RefreshToken);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _api.LogoutAsync(new LogoutRequest { RefreshToken = refreshToken }, ct);
        }

        await _signIn.SignOutAsync();
    }
}
