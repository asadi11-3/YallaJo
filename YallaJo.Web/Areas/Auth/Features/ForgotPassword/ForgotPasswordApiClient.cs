using YallaJo.Web.Areas.Auth.Features.ForgotPassword.Requests;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword;

/// <summary>
/// Thin API client for the self-service password-reset flow.
/// <para>
/// Phase 2C-5 — the former <c>ResendOtpAsync(Purpose="PasswordReset")</c>
/// helper was removed. Callers that want to re-send a reset code must
/// POST to <c>/forgot-password</c> again; the server-side
/// <c>ForgotPasswordCommand</c> applies its own 60-second throttle and
/// supersedes any prior active token, so it is safe and idempotent to
/// call on behalf of a user retry.
/// </para>
/// </summary>
public sealed class ForgotPasswordApiClient
{
    private readonly ApiClient _api;
    public ForgotPasswordApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request, CancellationToken ct = default)
        => _api.PostAsync<ForgotPasswordResponse>("/api/v1/auth/forgot-password", request, ct);
}
