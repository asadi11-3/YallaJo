using YallaJo.Web.Areas.Auth.Features.ResetPassword.Requests;
using YallaJo.Web.Areas.Auth.Features.ResetPassword.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.ResetPassword;

public sealed class ResetPasswordApiClient
{
    private readonly IApiClient _api;
    public ResetPasswordApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ResetPasswordResponse>> ResetPasswordAsync(
        ResetPasswordRequest request, CancellationToken ct = default)
        => _api.PostAsync<ResetPasswordResponse>("/api/v1/auth/reset-password", request, ct);
}
