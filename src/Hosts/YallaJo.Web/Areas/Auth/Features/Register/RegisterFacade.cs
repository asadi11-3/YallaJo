using YallaJo.Web.Areas.Auth.Features.Register.Mappers;
using YallaJo.Web.Areas.Auth.Features.Register.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.Register;

public sealed class RegisterFacade
{
    private readonly RegisterApiClient _api;

    public RegisterFacade(RegisterApiClient api) => _api = api;

    public async Task<RegisterResult> HandleAsync(RegisterVm vm, CancellationToken ct = default)
    {
        var request = RegisterMapper.ToRequest(vm);
        var result  = await _api.RegisterAsync(request, ct);

        if (result.IsSuccess)
            return RegisterResult.Ok(vm.Email.Trim());

        if (result.IsValidationError)
            return RegisterResult.Invalid(result.ValidationErrors!);

        var msg = result.StatusCode == 409
            ? "An account with this email already exists."
            : result.StatusCode == 429
                ? "Too many registration attempts. Please try again later."
                : result.Error ?? "Registration failed. Please try again.";

        return RegisterResult.Fail(msg);
    }
}
