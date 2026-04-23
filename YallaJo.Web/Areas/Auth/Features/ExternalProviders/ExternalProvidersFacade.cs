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
        bool emailVerifiedByProvider,
        string? firstName = null,
        string? lastName = null) =>
        _ticketBuilder.Build(
            provider, providerUserId, email, emailVerifiedByProvider, firstName, lastName);

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
            // Deliberately generic — server never reveals which auto-link /
            // auto-create safety gate tripped (see ExternalLoginCommandHandler
            // AutoLinkRefusalReason). The operator-side log does name the
            // exact reason.
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

    // The API auto-resolves external sign-in in three ways (existing link →
    // auto-link by verified email → auto-create new account). A 401 therefore
    // means the provider itself did not attest email verification, a safety
    // gate tripped on an existing account, or the auto-create attempt hit a
    // concurrent race. The client message intentionally reveals none of
    // these; the operator-side log names the exact AutoLinkRefusalReason.
    public static ExternalLoginOutcome NotLinked() =>
        new()
        {
            IsNotLinked = true,
            Error = "We couldn't sign you in with this provider. Please try again, or sign in with email and password."
        };
    public static ExternalLoginOutcome Fail(string error) => new() { Error = error };
}
