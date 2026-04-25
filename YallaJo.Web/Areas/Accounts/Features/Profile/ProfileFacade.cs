using YallaJo.Web.Areas.Accounts.Features.Profile.Mappers;
using YallaJo.Web.Areas.Accounts.Features.Profile.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.Features.Profile;

public sealed class ProfileFacade
{
    private readonly ProfileApiClient _api;
    private readonly IApiAssetUrlResolver _assetResolver;

    public ProfileFacade(ProfileApiClient api, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _assetResolver = assetResolver;
    }

    public async Task<ApiResult<ProfileVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetProfileAsync(ct);

        if (result.IsSuccess && result.Data is not null)
            return ApiResult<ProfileVm>.CreateSuccess(ProfileMapper.ToVm(result.Data, _assetResolver));

        if (result.IsUnauthorized) return ApiResult<ProfileVm>.ForceSignOut();
        if (result.IsNotFound)     return ApiResult<ProfileVm>.CreateFailure(404, "Profile not found.");
        return ApiResult<ProfileVm>.CreateFailure(result.StatusCode, result.Error ?? "Could not load profile.");
    }

    public async Task<ApiResult> UpdateAsync(UpdateProfileVm vm, CancellationToken ct = default)
    {
        var result = await _api.UpdateProfileAsync(ProfileMapper.ToUpdateRequest(vm), ct);

        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsNotFound)        return ApiResult.Fail("Profile not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not update profile.");
    }

    public async Task<ApiResult> UpdateAvatarAsync(UpdateAvatarVm vm, CancellationToken ct = default)
    {
        if (vm.File is null || vm.File.Length == 0)
            return ApiResult.Invalid(new Dictionary<string, string[]>
            {
                [nameof(UpdateAvatarVm.File)] = ["An image file is required."],
            });

        await using var stream = vm.File.OpenReadStream();
        var result = await _api.UpdateAvatarAsync(stream, vm.File.FileName, vm.File.ContentType, ct);

        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsNotFound)        return ApiResult.Fail("Profile not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not upload avatar.");
    }

    public async Task<ApiResult> DeleteAvatarAsync(CancellationToken ct = default)
    {
        var result = await _api.DeleteAvatarAsync(ct);

        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsNotFound)        return ApiResult.Fail("Profile not found.");
        return ApiResult.Fail(result.Error ?? "Could not remove avatar.");
    }

    public async Task<ApiResult> DeleteAsync(CancellationToken ct = default)
    {
        var result = await _api.DeleteProfileAsync(ct);

        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsNotFound)        return ApiResult.Fail("Profile not found.");
        return ApiResult.Fail(result.Error ?? "Could not delete profile.");
    }
}
