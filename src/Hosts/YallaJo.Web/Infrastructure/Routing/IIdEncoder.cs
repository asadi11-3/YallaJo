namespace YallaJo.Web.Infrastructure.Routing;

/// <summary>
/// Reversible, opaque encoding of entity <see cref="System.Guid"/> identifiers for
/// use in URLs, form values and links — so raw GUIDs are never exposed to the user
/// (UI-UX-Design.md Rule 5 / F10 defense-in-depth).
/// <para>
/// The encoding is <b>not</b> a security boundary on its own: the Admin area is already
/// gated by cookie auth + RBAC. Encoding removes guessable/enumerable raw IDs from the
/// address bar and from copy/paste surfaces. Authorization remains the real control.
/// </para>
/// <para>
/// <b>Backward compatibility:</b> <see cref="TryDecode"/> accepts BOTH an encoded token
/// AND a plain GUID string, so existing bookmarks / links keep resolving during the
/// transition. Only <see cref="Encode"/> (outbound) produces the opaque form, and it is
/// currently wired into the Admin area only.
/// </para>
/// </summary>
public interface IIdEncoder
{
    /// <summary>Encodes a GUID into a URL-safe opaque token.</summary>
    string Encode(Guid id);

    /// <summary>
    /// Decodes a token produced by <see cref="Encode"/>. Also accepts a plain GUID
    /// string (e.g. "3fa85f64-5717-...") for backward compatibility.
    /// Returns <c>false</c> for null/empty/unparseable input.
    /// </summary>
    bool TryDecode(string? value, out Guid id);

    /// <summary>
    /// Convenience wrapper around <see cref="TryDecode"/>; returns the decoded GUID or
    /// <c>null</c> when the value cannot be decoded.
    /// </summary>
    Guid? Decode(string? value);
}
