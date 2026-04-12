using YallaJo.Web.Areas.Auth.Features.Login.Mappers;
using YallaJo.Web.Areas.Auth.Features.Login.ViewModels;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.Login;

/// <summary>
/// Orchestrates the login flow:
///  1. Map VM → API request
///  2. Call backend
///  3. On success → establish the web session via IWebSignInService
///  4. Return a typed result so the controller stays thin
/// </summary>
public sealed class LoginFacade
{
    private readonly LoginApiClient    _api;
    private readonly IWebSignInService _signIn;

    public LoginFacade(LoginApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<LoginResult> HandleAsync(LoginVm vm, CancellationToken ct = default)
    {
        var request = LoginMapper.ToRequest(vm);
        var result  = await _api.LoginAsync(request, ct);

        if (result.IsSuccess)
        {
            var d = result.Data!;
            await _signIn.SignInAsync(d.UserId, d.AccessToken, d.RefreshToken, d.RefreshTokenExpiresAt);
            return LoginResult.Ok();
        }

        if (result.IsValidationError)
            return LoginResult.Invalid(result.ValidationErrors!);

        var msg = result.StatusCode == 401
            ? "Invalid email or password."
            : result.Error ?? "Login failed. Please try again.";

        return LoginResult.Fail(msg);
    }
}

// ── Facade result ─────────────────────────────────────────────────────────────

public sealed class LoginResult
{
    public bool                                      IsSuccess        { get; private init; }
    public string?                                   Error            { get; private init; }
    public IReadOnlyDictionary<string, string[]>?   ValidationErrors { get; private init; }

    public static LoginResult Ok()   => new() { IsSuccess = true };
    public static LoginResult Fail(string error)  => new() { IsSuccess = false, Error = error };
    public static LoginResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { IsSuccess = false, ValidationErrors = errors };
}
