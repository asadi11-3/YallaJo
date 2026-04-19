using YallaJo.Web.Areas.Auth.Features.Sessions.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.Sessions;

public sealed class SessionsApiClient
{
    private readonly ApiClient _api;
    public SessionsApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<List<SessionItemResponse>>> GetSessionsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<SessionItemResponse>>("/api/v1/auth/sessions", ct);

    public Task<ApiResult> RevokeSessionAsync(Guid sessionId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/auth/sessions/{sessionId}", ct);
}
