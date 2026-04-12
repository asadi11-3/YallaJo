using YallaJo.Web.Areas.Auth.Features.ForgotPassword.Requests;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword;

public sealed class ForgotPasswordApiClient
{
    private readonly ApiClient _api;
    public ForgotPasswordApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request, CancellationToken ct = default)
        => _api.PostAsync<ForgotPasswordResponse>("/api/v1/auth/forgot-password", request, ct);

    public Task<ApiResult> ResendOtpAsync(string email, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/resend-otp",
            new { Email = email, Purpose = "PasswordReset" }, ct);
}
