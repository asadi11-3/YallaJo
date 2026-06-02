using YallaJo.Web.Areas.Auth.Models.VerifyEmail;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.ApiClients;
public sealed class VerifyEmailApiClient
{
    private readonly IApiClient _api;
    public VerifyEmailApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<VerifyEmailResponse>> VerifyEmailAsync(
        VerifyEmailRequest request, CancellationToken ct = default)
        => _api.PostAsync<VerifyEmailResponse>("/api/v1/auth/verify-email", request, ct);

    public Task<ApiResult> ResendOtpAsync(
        string email,
        string purpose,
        string recaptchaToken,
        CancellationToken ct = default)
        => _api.PostAsync(
            "/api/v1/auth/resend-otp",
            new { Email = email, Purpose = purpose, RecaptchaToken = recaptchaToken },
            ct);
}
