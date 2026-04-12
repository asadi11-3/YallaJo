using YallaJo.Web.Areas.Auth.Features.Login.Requests;
using YallaJo.Web.Areas.Auth.Features.Login.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.Login;

/// <summary>
/// Wraps all backend calls needed by the Login feature.
/// Receives the shared ApiClient (which has JwtAuthHandler for token injection).
/// Login itself is anonymous — the handler is a no-op when no token is in the cookie.
/// </summary>
public sealed class LoginApiClient
{
    private readonly ApiClient _api;

    public LoginApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => _api.PostAsync<LoginResponse>("/api/v1/auth/login", request, ct);
}
