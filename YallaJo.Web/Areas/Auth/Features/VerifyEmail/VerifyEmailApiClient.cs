using YallaJo.Web.Areas.Auth.Features.VerifyEmail.Requests;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.VerifyEmail;

public sealed class VerifyEmailApiClient
{
    private readonly ApiClient _api;
    public VerifyEmailApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<VerifyEmailResponse>> VerifyEmailAsync(
        VerifyEmailRequest request, CancellationToken ct = default)
        => _api.PostAsync<VerifyEmailResponse>("/api/v1/auth/verify-email", request, ct);

    public Task<ApiResult> ResendOtpAsync(string email, string purpose, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/resend-otp", new { Email = email, Purpose = purpose }, ct);
}
