using YallaJo.Web.Areas.Auth.Features.Register.Requests;
using YallaJo.Web.Areas.Auth.Features.Register.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.Register;

public sealed class RegisterApiClient
{
    private readonly ApiClient _api;

    public RegisterApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => _api.PostAsync<RegisterResponse>("/api/v1/auth/register", request, ct);
}
