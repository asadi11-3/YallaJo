using YallaJo.Web.Areas.Auth.Models.ExternalProviders;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Areas.Auth.ApiClients;

namespace YallaJo.Web.Areas.Auth.Facades;
public sealed class ExternalProvidersFacade
{
    private readonly ExternalProvidersApiClient _api;
    private readonly IWebSignInService          _signIn;
    private readonly IExternalAuthTicketBuilder _ticketBuilder;

    public ExternalProvidersFacade(
        ExternalProvidersApiClient api,
        IWebSignInService signIn,
        IExternalAuthTicketBuilder ticketBuilder)
    {
        _api           = api;
        _signIn        = signIn;
        _ticketBuilder = ticketBuilder;
    }

    /// <summary>
    /// Fetches the user's active linked providers (B5) mapped to display VMs with the
    /// provider e-mail already masked. 401 signs the local cookie out; any other failure
    /// returns Fail so the page can degrade to link-buttons-only instead of crashing.
    /// </summary>
    public async Task<ApiResult<IReadOnlyList<LinkedProviderVm>>> GetLinkedAsync(
        CancellationToken ct = default)
    {
        var result = await _api.GetLinkedAsync(ct);

        if (result.IsSuccess)
        {
            IReadOnlyList<LinkedProviderVm> vms = (result.Data ?? [])
                .Select(r => new LinkedProviderVm
                {
                    ProviderId  = r.ProviderId,
                    Provider    = r.Provider,
                    MaskedEmail = MaskEmail(r.ProviderEmail),
                    LinkedAt    = r.LinkedAt,
                })
                .ToList();
            return ApiResult<IReadOnlyList<LinkedProviderVm>>.Ok(vms);
        }

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult<IReadOnlyList<LinkedProviderVm>>.ForceSignOut();
        }

        return ApiResult<IReadOnlyList<LinkedProviderVm>>.Fail(
            result.StatusCode, result.Error ?? "Could not load linked providers.");
    }

    /// <summary>"j***@example.com" — limits PII on screen; empty when no e-mail.</summary>
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        return $"{email[0]}***{email[at..]}";
    }

    public string BuildTicket(
        string provider,
        string providerUserId,
        string? email,
        bool emailVerifiedByProvider,
        string? firstName = null,
        string? lastName = null) =>
        _ticketBuilder.Build(
            provider, providerUserId, email, emailVerifiedByProvider, firstName, lastName);

    public async Task<ApiResult> LinkAsync(
        string ticket,
        string recaptchaToken,
        CancellationToken ct = default)
    {
        var result = await _api.LinkAsync(
        new LinkExternalProviderRequest
        {
            Ticket = ticket,
            RecaptchaToken = recaptchaToken,
        }, ct);

        if (result.IsSuccess) return ApiResult.Ok();

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        if (result.IsConflict)
            return ApiResult.Fail(result.Error ?? "This provider account is already linked.");

        if (result.IsValidationError)
            return ApiResult.Invalid(result.ValidationErrors!);

        return ApiResult.Fail(result.Error ?? "Link failed.");
    }

    public async Task<ExternalLoginOutcome> LoginAsync(
        string ticket,
        string recaptchaToken,
        CancellationToken ct = default)
    {
        var result = await _api.LoginAsync(
        new ExternalLoginRequest
        {
            Ticket = ticket,
            RecaptchaToken = recaptchaToken,
        }, ct);

        if (result.IsSuccess && result.Data is not null)
        {
            var d = result.Data;
            await _signIn.SignInAsync(d.UserId, d.AccessToken, d.RefreshToken, d.RefreshTokenExpiresAt);
            return ExternalLoginOutcome.Ok();
        }

        if (result.IsUnauthorized)
            return ExternalLoginOutcome.NotLinked();

        if (result.IsTooManyRequests)
            return ExternalLoginOutcome.Fail("Too many sign-in attempts. Please try again shortly.");

        return ExternalLoginOutcome.Fail(result.Error ?? "External sign-in failed.");
    }

    public async Task<ApiResult> UnlinkAsync(Guid providerId, CancellationToken ct = default)
    {
        var result = await _api.UnlinkAsync(providerId, ct);

        if (result.IsSuccess || result.IsNotFound) return ApiResult.Ok();

        if (result.IsUnauthorized)
        {
            await _signIn.SignOutAsync();
            return ApiResult.ForceSignOut();
        }

        // B6: 409 = last-login-method guard (or a concurrency conflict) — preserve the
        // status so the controller can show the localized lockout explanation.
        if (result.IsConflict)
            return ApiResult.Fail(409, result.Error);

        return ApiResult.Fail(result.Error ?? "Unlink failed.");
    }
}
