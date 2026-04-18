using YallaJo.Web.Areas.Accounts.Features.ChangePassword.Mappers;
using YallaJo.Web.Areas.Accounts.Features.ChangePassword.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Features.ChangePassword;

public sealed class ChangePasswordFacade
{
    private readonly ChangePasswordApiClient _api;
    public ChangePasswordFacade(ChangePasswordApiClient api) => _api = api;

    public async Task<ApiResult> HandleAsync(ChangePasswordVm vm, CancellationToken ct = default)
    {
        var result = await _api.ChangePasswordAsync(ChangePasswordMapper.ToRequest(vm), ct);

        if (result.IsSuccess)           return ApiResult.Ok();
        if (result.IsUnauthorized)      return ApiResult.ForceSignOut();
        if (result.IsValidationError)   return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not change password.");
    }
}
