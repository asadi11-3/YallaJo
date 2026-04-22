using YallaJo.Web.Areas.Auth.Features.ExternalProviders.Requests;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders;

/// <summary>
/// BFF-side orchestrator for external-provider operations. Responsibilities:
/// <list type="bullet">
///   <item><description>Mint a signed, single-use ticket from a verified
///   provider identity (already authenticated by ASP.NET Core's external
///   authentication handlers) and POST it to the API.</description></item>
///   <item><description>Sign the user into the browser cookie on successful
///   external login — identical to the password-login pipeline.</description></item>
///   <item><description>Handle unlink calls against the API.</description></item>
/// </list>
/// </summary>
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
    /// Builds a signed external-provider ticket for an already-verified identity.
    /// The ticket is intended to be POSTed back from an interstitial view that
    /// also supplies a freshly-minted reCAPTCHA token.
    /// </summary>
    public string BuildTicket(
        string provider,
        string providerUserId,
        string? email,
        bool emailVerifiedByProvider) =>
        _ticketBuilder.Build(provider, providerUserId, email, emailVerifiedByProvider);

    /// <summary>
    /// Link the external identity to the currently-signed-in user. Caller MUST
    /// ensure the cookie auth user matches the user who initiated the challenge.
    /// </summary>
    public async Task<ApiResult> LinkAsync(
        string ticket,
        string recaptchaToken,
        CancellationToken ct = default)
    {
        var result = await _api.LinkAsync(new LinkExternalProviderRequest
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

    /// <summary>
    /// Exchange a verified external identity for a first-party session.
    /// On success, writes the authentication cookie so the user is logged in.
    /// </summary>
    public async Task<ExternalLoginOutcome> LoginAsync(
        string ticket,
        string recaptchaToken,
        CancellationToken ct = default)
    {
        var result = await _api.LoginAsync(new ExternalLoginRequest
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
        {
            // Deliberately generic — server doesn't reveal whether the provider
            // account is known. Encourages the user to sign in normally and link.
            return ExternalLoginOutcome.NotLinked();
        }

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

        return ApiResult.Fail(result.Error ?? "Unlink failed.");
    }
}

public sealed class ExternalLoginOutcome
{
    public bool IsSuccess { get; private init; }
    public bool IsNotLinked { get; private init; }
    public string? Error { get; private init; }

    public static ExternalLoginOutcome Ok() => new() { IsSuccess = true };
    public static ExternalLoginOutcome NotLinked() =>
        new()
        {
            IsNotLinked = true,
            Error = "No account is linked to this provider. Sign in normally, then link the provider from your profile."
        };
    public static ExternalLoginOutcome Fail(string error) => new() { Error = error };
}
