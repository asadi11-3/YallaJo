using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class DeleteFacade
{
    private readonly DeleteApiClient _api;
    private readonly IWebSignInService _signIn;

    public DeleteFacade(DeleteApiClient api, IWebSignInService signIn)
    {
        _api = api;
        _signIn = signIn;
    }

    public async Task<ApiResult> DeleteAsync(CancellationToken ct = default)
    {
        var result = await _api.DeleteProfileAsync(ct);

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        if (!result.IsSuccess)
        {
            return result.IsNotFound
                ? ApiResult.Fail(result.StatusCode, "Your account could not be found.")
                : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not delete your account.");
        }

        // Soft-delete succeeded: clear the local auth cookie so the session ends.
        await _signIn.SignOutAsync();
        return ApiResult.Ok(result.StatusCode);
    }

    public async Task<ApiResult> RestoreAsync(CancellationToken ct = default)
    {
        var result = await _api.RestoreProfileAsync(ct);

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        return result.IsSuccess
            ? ApiResult.Ok(result.StatusCode)
            : result.IsNotFound
                ? ApiResult.Fail(result.StatusCode, "There is no account to restore.")
                : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not restore your account.");
    }
}
