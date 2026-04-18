using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.Requests;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles;

public sealed class ProfilesApiClient
{
    private readonly ApiClient _api;
    public ProfilesApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<Guid>> CreateProfileAsync(
        CreateProfileRequest request, CancellationToken ct = default)
        => _api.PostAsync<Guid>("/api/v1/accounts/profiles", request, ct);
}
