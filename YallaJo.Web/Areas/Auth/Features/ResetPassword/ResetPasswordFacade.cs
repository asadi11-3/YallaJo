using YallaJo.Web.Areas.Auth.Features.ResetPassword.Mappers;
using YallaJo.Web.Areas.Auth.Features.ResetPassword.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.ResetPassword;

public sealed class ResetPasswordFacade
{
    private readonly ResetPasswordApiClient _api;
    public ResetPasswordFacade(ResetPasswordApiClient api) => _api = api;

    public async Task<ResetPasswordResult> HandleAsync(ResetPasswordVm vm, CancellationToken ct = default)
    {
        var result = await _api.ResetPasswordAsync(ResetPasswordMapper.ToRequest(vm), ct);

        if (result.IsSuccess)
            return ResetPasswordResult.Ok();

        if (result.IsValidationError)
            return ResetPasswordResult.Invalid(result.ValidationErrors!);

        return ResetPasswordResult.Fail(result.Error ?? "Password reset failed.");
    }
}

public sealed class ResetPasswordResult
{
    public bool                                   IsSuccess        { get; private init; }
    public string?                                Error            { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static ResetPasswordResult Ok()   => new() { IsSuccess = true };
    public static ResetPasswordResult Fail(string e)  => new() { IsSuccess = false, Error = e };
    public static ResetPasswordResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { IsSuccess = false, ValidationErrors = errors };
}
