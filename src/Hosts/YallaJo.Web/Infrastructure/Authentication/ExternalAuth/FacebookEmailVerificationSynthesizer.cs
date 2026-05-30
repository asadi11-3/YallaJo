using System.Security.Claims;

namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Meta does not expose a <c>email_verified</c> flag like Google's OpenID
/// Connect userinfo payload does. However, Meta <b>only</b> releases the email
/// field once the user has confirmed it on their Facebook account — so an
/// email actually being present in the identity IS Meta's verification
/// signal. This tiny helper normalizes that behaviour: it adds
/// <c>email_verified=true</c> to the external <see cref="ClaimsIdentity"/>
/// when (and only when) Meta surfaced an email.
///
/// <para>
/// Extracted into its own class so it can be unit tested without having to
/// build a fully-formed <c>OAuthCreatingTicketContext</c>.
/// </para>
/// </summary>
public static class FacebookEmailVerificationSynthesizer
{
    /// <summary>
    /// Synthesizes <c>email_verified=true</c> on the provided identity when
    /// it already carries an email claim and does not already carry an
    /// <c>email_verified</c> claim. No-ops otherwise.
    /// </summary>
    public static void Apply(ClaimsIdentity? identity)
    {
        if (identity is null) return;

        var email = identity.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email)) return;

        if (identity.FindFirst("email_verified") is not null) return;

        identity.AddClaim(new Claim("email_verified", "true", ClaimValueTypes.Boolean));
    }
}
