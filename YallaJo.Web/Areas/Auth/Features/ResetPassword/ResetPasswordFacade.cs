using YallaJo.Web.Areas.Auth.Features.ResetPassword.Mappers;
using YallaJo.Web.Areas.Auth.Features.ResetPassword.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Auth.Features.ResetPassword;

public sealed class ResetPasswordFacade
{
    private readonly ResetPasswordApiClient _api;
    public ResetPasswordFacade(ResetPasswordApiClient api) => _api = api;

    public async Task<ApiResult> HandleAsync(ResetPasswordVm vm, CancellationToken ct = default)
    {
        var result = await _api.ResetPasswordAsync(ResetPasswordMapper.ToRequest(vm), ct);

        if (result.IsSuccess)
            return ApiResult.Ok();

        if (result.IsValidationError)
            return ApiResult.Invalid(result.ValidationErrors!);

        return ApiResult.Fail(result.Error ?? "Password reset failed.");
    }
}
