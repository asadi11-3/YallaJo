using Microsoft.AspNetCore.DataProtection;

namespace YallaJo.Web.Infrastructure.Authentication.SignIn;

/// <summary>
/// Server-trusted carrier for the "registration succeeded, e-mail OTP verification
/// pending" intermediate state.
/// <para>
/// Previously the pending identity travelled as a plain <c>?email=</c> query parameter
/// and a hidden form field — PII in browser history/server logs, and an open invitation
/// for anyone to load <c>/auth/two-factor-auth?email=victim@x</c> and brute-force OTPs
/// for an arbitrary account (bounded only by API rate limits + reCAPTCHA).
/// </para>
/// <para>
/// Now the e-mail is sealed inside a Data-Protection-encrypted, time-limited cookie that
/// only this host can read. The OTP itself is NEVER stored here — it lives exclusively
/// API-side. The cookie is HttpOnly + Secure + SameSite=Lax, scoped to <c>/auth</c>, and
/// expires after <see cref="Lifetime"/> both via the cookie Max-Age and, authoritatively,
/// via <see cref="ITimeLimitedDataProtector"/> (a replayed cookie value fails to unprotect
/// after expiry even if the browser kept it).
/// </para>
/// </summary>
public interface IPendingVerificationStore
{
    /// <summary>Issues the pending-verification cookie for <paramref name="email"/>.</summary>
    void Issue(string email);

    /// <summary>
    /// Returns the pending e-mail, or <c>null</c> when the cookie is missing, expired,
    /// or fails authentication (tampered / wrong key ring).
    /// </summary>
    string? TryRead();

    /// <summary>Deletes the cookie (after successful verification or abandonment).</summary>
    void Clear();
}

public sealed class PendingVerificationStore : IPendingVerificationStore
{
    /// <summary>
    /// Must comfortably cover the API-side OTP TTL plus a resend or two. Short enough
    /// that an abandoned cookie is worthless quickly.
    /// </summary>
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    private const string CookieName = "YallaJo.PendingVerify";
    private const string Purpose    = "YallaJo.Auth.PendingVerification.v1";

    private readonly ITimeLimitedDataProtector _protector;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PendingVerificationStore(
        IDataProtectionProvider dataProtection,
        IHttpContextAccessor httpContextAccessor)
    {
        _protector = dataProtection.CreateProtector(Purpose).ToTimeLimitedDataProtector();
        _httpContextAccessor = httpContextAccessor;
    }

    public void Issue(string email)
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP context.");

        var payload = _protector.Protect(email.Trim(), Lifetime);

        context.Response.Cookies.Append(CookieName, payload, new CookieOptions
        {
            HttpOnly    = true,
            Secure      = true,
            SameSite    = SameSiteMode.Lax,
            // Scoped to the auth funnel only — the cookie never rides along on
            // unrelated requests.
            Path        = "/auth",
            MaxAge      = Lifetime,
            IsEssential = true, // functional auth-flow cookie, not tracking
        });
    }

    public string? TryRead()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return null;

        if (!context.Request.Cookies.TryGetValue(CookieName, out var payload)
            || string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(payload);
        }
        catch (Exception)
        {
            // Expired, tampered, or protected under a rotated/foreign key — treat all
            // identically as "no pending verification".
            return null;
        }
    }

    public void Clear()
    {
        var context = _httpContextAccessor.HttpContext;
        context?.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path     = "/auth",
            Secure   = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
        });
    }
}
