using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles;

public sealed class ProfilesFacade
{
    private readonly ProfilesApiClient _api;
    public ProfilesFacade(ProfilesApiClient api) => _api = api;

    public async Task<ApiResult<Guid>> CreateAsync(CreateProfileVm vm, CancellationToken ct = default)
    {
        var result = await _api.CreateProfileAsync(ProfilesMapper.ToCreateRequest(vm), ct);

        if (result.IsSuccess && result.Data != Guid.Empty)
            return ApiResult<Guid>.CreateSuccess(result.Data, result.StatusCode);

        if (result.IsUnauthorized)    return ApiResult<Guid>.ForceSignOut();
        if (result.IsConflict)        return ApiResult<Guid>.CreateFailure(409, "Profile already exists for this user.");
        if (result.IsNotFound)        return ApiResult<Guid>.CreateFailure(404, "Security user not found.");
        if (result.IsValidationError) return ApiResult<Guid>.CreateValidationFailure(result.StatusCode, result.ValidationErrors!);
        return ApiResult<Guid>.CreateFailure(result.StatusCode, result.Error ?? "Could not create profile.");
    }
}
