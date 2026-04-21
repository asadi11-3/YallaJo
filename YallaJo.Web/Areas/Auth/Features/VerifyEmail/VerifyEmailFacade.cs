using YallaJo.Web.Areas.Auth.Features.VerifyEmail.Mappers;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
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

    public async Task<ApiResult> HandleAsync(VerifyEmailVm vm, CancellationToken ct = default)
    {
        var request = VerifyEmailMapper.ToRequest(vm);
        var result  = await _api.VerifyEmailAsync(request, ct);

        if (result.IsSuccess)
        {
            var d = result.Data!;
            await _signIn.SignInAsync(d.UserId, d.AccessToken, d.RefreshToken, d.RefreshTokenExpiresAt);
            return ApiResult.Ok();
        }

        if (result.IsValidationError)
            return ApiResult.Invalid(result.ValidationErrors!);

        return ApiResult.Fail(result.Error ?? "Email verification failed.");
    }

    public async Task<ApiResult> ResendOtpAsync(string email, string purpose, CancellationToken ct = default)
    {
        var result = await _api.ResendOtpAsync(email, purpose, ct);
        if (result.IsSuccess)
            return ApiResult.Ok(result.StatusCode);

        if (result.IsValidationError)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors!);

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not resend code.");
    }
}
