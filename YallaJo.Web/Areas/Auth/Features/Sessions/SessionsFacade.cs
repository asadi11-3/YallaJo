using YallaJo.Web.Areas.Auth.Features.Sessions.Mappers;
using YallaJo.Web.Areas.Auth.Features.Sessions.ViewModels;
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

    public async Task<SessionsFacadeResult> GetSessionsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetSessionsAsync(ct);

        if (result.IsSuccess)
        {
            var vm = new SessionsVm
            {
                Sessions = (result.Data ?? []).Select(SessionsMapper.ToVm).ToList()
            };
            return SessionsFacadeResult.Ok(vm);
        }

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return SessionsFacadeResult.ForceSignOut();
        }

        return SessionsFacadeResult.Fail(result.Error ?? "Could not load sessions.");
    }

    public async Task<SessionsFacadeResult> RevokeAsync(Guid sessionId, CancellationToken ct = default)
    {
        var result = await _api.RevokeSessionAsync(sessionId, ct);

        if (result.IsSuccess || result.IsNotFound)
            return SessionsFacadeResult.Ok(null!);

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return SessionsFacadeResult.ForceSignOut();
        }

        return SessionsFacadeResult.Fail(result.Error ?? "Revoke failed.");
    }
}

public sealed class SessionsFacadeResult
{
    public bool        IsSuccess     { get; private init; }
    public string?     Error         { get; private init; }
    public bool        RequireSignOut{ get; private init; }
    public SessionsVm? Data          { get; private init; }

    public static SessionsFacadeResult Ok(SessionsVm data)  => new() { IsSuccess = true,  Data = data };
    public static SessionsFacadeResult Fail(string e)        => new() { IsSuccess = false, Error = e };
    public static SessionsFacadeResult ForceSignOut()        => new() { IsSuccess = false, RequireSignOut = true };
}
