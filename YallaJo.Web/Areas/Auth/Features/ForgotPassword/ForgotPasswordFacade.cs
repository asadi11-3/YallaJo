using YallaJo.Web.Areas.Auth.Features.ForgotPassword.Mappers;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword;

public sealed class ForgotPasswordFacade
{
    private readonly ForgotPasswordApiClient _api;
    public ForgotPasswordFacade(ForgotPasswordApiClient api) => _api = api;

    public async Task<ForgotPasswordResult> HandleAsync(ForgotPasswordVm vm, CancellationToken ct = default)
    {
        var result = await _api.ForgotPasswordAsync(ForgotPasswordMapper.ToRequest(vm), ct);

        // Always report "we sent a code if the account exists" to avoid
        // leaking whether the email is registered.
        if (result.IsSuccess || result.IsNotFound)
            return ForgotPasswordResult.Ok();

        if (result.IsValidationError)
            return ForgotPasswordResult.Invalid(result.ValidationErrors!);

        return ForgotPasswordResult.Fail(result.Error ?? "Request failed.");
    }
}

public sealed class ForgotPasswordResult
{
    public bool                                   IsSuccess        { get; private init; }
    public string?                                Error            { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static ForgotPasswordResult Ok()   => new() { IsSuccess = true };
    public static ForgotPasswordResult Fail(string e)  => new() { IsSuccess = false, Error = e };
    public static ForgotPasswordResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { IsSuccess = false, ValidationErrors = errors };
}
