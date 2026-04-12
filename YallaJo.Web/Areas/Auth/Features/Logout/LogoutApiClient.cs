using YallaJo.Web.Areas.Auth.Features.Logout.Requests;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.Logout;

public sealed class LogoutApiClient
{
    private readonly ApiClient _api;
    public LogoutApiClient(ApiClient api) => _api = api;

    /// <summary>POST /api/v1/auth/logout — requires Bearer + RefreshToken in body.</summary>
    public Task<ApiResult> LogoutAsync(LogoutRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/logout", request, ct);
}
