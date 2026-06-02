using YallaJo.Web.Areas.Auth.Models.Register;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.ApiClients;
public sealed class RegisterApiClient
{
    private readonly IApiClient _api;

    public RegisterApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => _api.PostAsync<RegisterResponse>("/api/v1/auth/register", request, ct);
}
