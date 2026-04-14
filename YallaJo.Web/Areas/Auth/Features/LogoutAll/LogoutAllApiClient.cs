using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.LogoutAll;

public sealed class LogoutAllApiClient
{
    private readonly ApiClient _api;
    public LogoutAllApiClient(ApiClient api) => _api = api;

    public Task<ApiResult> LogoutAllAsync(CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/logout-all", null, ct);
}
