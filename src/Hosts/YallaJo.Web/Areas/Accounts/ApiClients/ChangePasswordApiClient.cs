using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class ChangePasswordApiClient
{
    private readonly IApiClient _api;
    public ChangePasswordApiClient(IApiClient api) => _api = api;

    public Task<ApiResult> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken ct = default)
        => _api.PutAsync("/api/v1/security/account/password", request, ct);
}
