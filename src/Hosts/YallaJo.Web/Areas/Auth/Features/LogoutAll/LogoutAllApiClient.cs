using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.LogoutAll;

public sealed class LogoutAllApiClient
{
    private readonly IApiClient _api;
    public LogoutAllApiClient(IApiClient api) => _api = api;

    public Task<ApiResult> LogoutAllAsync(CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/logout-all", null, ct);
}
