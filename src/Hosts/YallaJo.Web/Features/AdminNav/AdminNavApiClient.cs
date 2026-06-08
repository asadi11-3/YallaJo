using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Features.AdminNav;

/// <summary>
/// Thin HTTP client for the current user's DB-backed security snapshot
/// (Security module, GET /api/v1/security/me).
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'ApiClient' suffix.
/// </summary>
public sealed class AdminNavApiClient
{
    private const string MeEndpoint = "/api/v1/security/me";

    private readonly IApiClient _api;

    public AdminNavApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<SecurityMeResponse>> GetMeAsync(CancellationToken ct = default)
        => _api.GetAsync<SecurityMeResponse>(MeEndpoint, ct);
}
