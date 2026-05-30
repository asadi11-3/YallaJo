using YallaJo.Web.Areas.Auth.Features.ResetPassword.Requests;
using YallaJo.Web.Areas.Auth.Features.ResetPassword.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.ResetPassword;

public sealed class ResetPasswordApiClient
{
    private readonly ApiClient _api;
    public ResetPasswordApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<ResetPasswordResponse>> ResetPasswordAsync(
        ResetPasswordRequest request, CancellationToken ct = default)
        => _api.PostAsync<ResetPasswordResponse>("/api/v1/auth/reset-password", request, ct);
}
