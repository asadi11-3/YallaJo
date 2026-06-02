using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class UpdatePhoneFacade
{
    private readonly UpdatePhoneApiClient _api;
    public UpdatePhoneFacade(UpdatePhoneApiClient api) => _api = api;

    public async Task<ApiResult> HandleAsync(UpdatePhoneVm vm, CancellationToken ct = default)
    {
        var result = await _api.UpdatePrimaryPhoneAsync(UpdatePhoneMapper.ToRequest(vm), ct);

        if (result.IsSuccess)           return ApiResult.Ok();
        if (result.IsUnauthorized)      return ApiResult.ForceSignOut();
        if (result.IsNotFound)          return ApiResult.Fail("User not found.");
        if (result.IsValidationError)   return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not update phone number.");
    }
}
