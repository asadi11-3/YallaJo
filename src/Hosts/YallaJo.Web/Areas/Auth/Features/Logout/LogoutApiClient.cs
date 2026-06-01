using YallaJo.Web.Areas.Auth.Features.Logout.Requests;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.Logout;

public sealed class LogoutApiClient
{
    private readonly IApiClient _api;
    public LogoutApiClient(IApiClient api) => _api = api;
    public Task<ApiResult> LogoutAsync(LogoutRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/logout", request, ct);
}
