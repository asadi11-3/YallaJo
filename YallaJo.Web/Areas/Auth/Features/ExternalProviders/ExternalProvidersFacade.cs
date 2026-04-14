using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

public sealed class ExternalProvidersFacade
{
    private readonly ExternalProvidersApiClient _api;
    private readonly IWebSignInService          _signIn;

    public ExternalProvidersFacade(ExternalProvidersApiClient api, IWebSignInService signIn)
    {
        _api    = api;
        _signIn = signIn;
    }

    public async Task<ApiResult> LinkAsync(
        ExternalProvidersVm vm, CancellationToken ct = default)
    {
        var request = new LinkExternalProviderRequest
        {
            Provider       = vm.Provider.Trim(),
            ProviderUserId = vm.ProviderUserId.Trim(),
            ProviderEmail  = vm.ProviderEmail?.Trim(),
        };

        var result = await _api.LinkAsync(request, ct);

        if (result.IsSuccess) return ApiResult.Ok("Provider linked successfully.");

        if (result.IsUnauthorized) { await _signIn.SignOutAsync(); return ApiResult.ForceSignOut(); }
        if (result.IsConflict)     return ApiResult.Fail("This provider account is already linked.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Link failed.");
    }

    public async Task<ApiResult> UnlinkAsync(Guid providerId, CancellationToken ct = default)
    {
        var result = await _api.UnlinkAsync(providerId, ct);

        if (result.IsSuccess || result.IsNotFound) return ApiResult.Ok("Provider unlinked.");

        if (result.IsUnauthorized) { await _signIn.SignOutAsync(); return ApiResult.ForceSignOut(); }

        return ApiResult.Fail(result.Error ?? "Unlink failed.");
    }
}


