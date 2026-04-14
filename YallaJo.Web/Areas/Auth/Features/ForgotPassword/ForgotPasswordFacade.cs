using YallaJo.Web.Areas.Auth.Features.ForgotPassword.Mappers;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword;

public sealed class ForgotPasswordFacade
{
    private readonly ForgotPasswordApiClient _api;
    public ForgotPasswordFacade(ForgotPasswordApiClient api) => _api = api;

    public async Task<ApiResult> HandleAsync(ForgotPasswordVm vm, CancellationToken ct = default)
    {
        var result = await _api.ForgotPasswordAsync(ForgotPasswordMapper.ToRequest(vm), ct);

        // Always report "we sent a code if the account exists" to avoid
        // leaking whether the email is registered.
        if (result.IsSuccess || result.IsNotFound)
            return ApiResult.Ok();

        if (result.IsValidationError)
            return ApiResult.Invalid(result.ValidationErrors!);

        return ApiResult.Fail(result.Error ?? "Request failed.");
    }
}

