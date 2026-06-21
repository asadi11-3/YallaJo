namespace ContentCore.Application.Promotions;

/// <summary>
/// Centralised safety policy for promotional CTA links.
/// <para>
/// Per the My Profile inline-promo security decision, a CTA link may only be:
/// <list type="bullet">
///   <item>a site-relative path beginning with a single '/' (NOT '//', which is protocol-relative), or</item>
///   <item>an absolute http/https URL.</item>
/// </list>
/// Everything else — <c>javascript:</c>, <c>data:</c>, <c>vbscript:</c>, <c>file:</c>,
/// protocol-relative <c>//host</c>, fragments/backslashes used for open-redirect or XSS —
/// is rejected. This is enforced server-side in command validation AND should be
/// re-checked at render time before emitting an <c>href</c>.
/// </para>
/// </summary>
public static class PromoLinkPolicy
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="url"/> is a safe CTA link.
    /// Null/empty is considered valid (the CTA is simply optional).
    /// </summary>
    public static bool IsSafe(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        var value = url.Trim();

        // Reject control characters / embedded newlines used to smuggle schemes.
        if (value.Any(c => char.IsControl(c)))
        {
            return false;
        }

        // Protocol-relative ("//evil.com") and backslash variants are open-redirect vectors.
        if (value.StartsWith("//", StringComparison.Ordinal) ||
            value.StartsWith("/\\", StringComparison.Ordinal) ||
            value.StartsWith("\\", StringComparison.Ordinal))
        {
            return false;
        }

        // Site-relative path: a single leading '/'.
        if (value.StartsWith('/'))
        {
            return true;
        }

        // Absolute URL: only http/https allowed; everything else (javascript:, data:, etc.) rejected.
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                   uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        // Not absolute and not a clean site-relative path => reject.
        return false;
    }

    /// <summary>
    /// Returns the URL when safe; otherwise <c>null</c>. Intended for render-time use so a
    /// poisoned value that somehow reached the database is never emitted as an href.
    /// </summary>
    public static string? Sanitize(string? url)
        => IsSafe(url) ? url?.Trim() : null;
}
