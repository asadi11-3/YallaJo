using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class DeleteApiClient
{
    private readonly IApiClient _api;
    public DeleteApiClient(IApiClient api) => _api = api;

    public Task<ApiResult> DeleteProfileAsync(CancellationToken ct = default)
        => _api.DeleteAsync("/api/v1/accounts/profile", ct);

    public Task<ApiResult> RestoreProfileAsync(CancellationToken ct = default)
        => _api.PostAsync("/api/v1/accounts/profile/restore", null, ct);
}
