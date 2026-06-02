using YallaJo.Web.Areas.Auth.Models.Login;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.ApiClients;
public sealed class LoginApiClient
{
    private readonly IApiClient _api;

    public LoginApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => _api.PostAsync<LoginResponse>("/api/v1/auth/login", request, ct);
}
