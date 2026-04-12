using YallaJo.Web.Areas.Auth.Features.VerifyEmail.Mappers;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail.ViewModels;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.VerifyEmail;

public sealed class VerifyEmailFacade
{
    private readonly VerifyEmailApiClient _api;
    private readonly IWebSignInService    _signIn;

    public VerifyEmailFacade(VerifyEmailApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<VerifyEmailResult> HandleAsync(VerifyEmailVm vm, CancellationToken ct = default)
    {
        var request = VerifyEmailMapper.ToRequest(vm);
        var result  = await _api.VerifyEmailAsync(request, ct);

        if (result.IsSuccess)
        {
            var d = result.Data!;
            await _signIn.SignInAsync(d.UserId, d.AccessToken, d.RefreshToken, d.RefreshTokenExpiresAt);
            return VerifyEmailResult.Ok();
        }

        if (result.IsValidationError)
            return VerifyEmailResult.Invalid(result.ValidationErrors!);

        return VerifyEmailResult.Fail(result.Error ?? "Email verification failed.");
    }

    public async Task<string?> ResendOtpAsync(string email, string purpose, CancellationToken ct = default)
    {
        var result = await _api.ResendOtpAsync(email, purpose, ct);
        return result.IsSuccess ? null : result.Error ?? "Could not resend code.";
    }
}

// ── Facade result ─────────────────────────────────────────────────────────────

public sealed class VerifyEmailResult
{
    public bool                                    IsSuccess        { get; private init; }
    public string?                                 Error            { get; private init; }
    public IReadOnlyDictionary<string, string[]>?  ValidationErrors { get; private init; }

    public static VerifyEmailResult Ok()   => new() { IsSuccess = true };
    public static VerifyEmailResult Fail(string error)  => new() { IsSuccess = false, Error = error };
    public static VerifyEmailResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { IsSuccess = false, ValidationErrors = errors };
}
