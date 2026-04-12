using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;
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

    public async Task<ExternalProvidersFacadeResult> LinkAsync(
        ExternalProvidersVm vm, CancellationToken ct = default)
    {
        var request = new LinkExternalProviderRequest
        {
            Provider       = vm.Provider.Trim(),
            ProviderUserId = vm.ProviderUserId.Trim(),
            ProviderEmail  = vm.ProviderEmail?.Trim(),
        };

        var result = await _api.LinkAsync(request, ct);

        if (result.IsSuccess) return ExternalProvidersFacadeResult.Ok("Provider linked successfully.");

        if (result.IsUnauthorized) { await _signIn.SignOutAsync(); return ExternalProvidersFacadeResult.ForceSignOut(); }
        if (result.IsConflict)     return ExternalProvidersFacadeResult.Fail("This provider account is already linked.");
        if (result.IsValidationError) return ExternalProvidersFacadeResult.Invalid(result.ValidationErrors!);

        return ExternalProvidersFacadeResult.Fail(result.Error ?? "Link failed.");
    }

    public async Task<ExternalProvidersFacadeResult> UnlinkAsync(Guid providerId, CancellationToken ct = default)
    {
        var result = await _api.UnlinkAsync(providerId, ct);

        if (result.IsSuccess || result.IsNotFound) return ExternalProvidersFacadeResult.Ok("Provider unlinked.");

        if (result.IsUnauthorized) { await _signIn.SignOutAsync(); return ExternalProvidersFacadeResult.ForceSignOut(); }

        return ExternalProvidersFacadeResult.Fail(result.Error ?? "Unlink failed.");
    }
}

public sealed class ExternalProvidersFacadeResult
{
    public bool                                   IsSuccess        { get; private init; }
    public string?                                Message          { get; private init; }
    public string?                                Error            { get; private init; }
    public bool                                   RequireSignOut   { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static ExternalProvidersFacadeResult Ok(string msg)   => new() { IsSuccess = true, Message = msg };
    public static ExternalProvidersFacadeResult Fail(string e)   => new() { IsSuccess = false, Error = e };
    public static ExternalProvidersFacadeResult ForceSignOut()   => new() { IsSuccess = false, RequireSignOut = true };
    public static ExternalProvidersFacadeResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { IsSuccess = false, ValidationErrors = errors };
}
