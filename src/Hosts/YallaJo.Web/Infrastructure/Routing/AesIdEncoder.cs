using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace YallaJo.Web.Infrastructure.Routing;

/// <summary>
/// <see cref="IIdEncoder"/> implementation that encrypts the 16 raw bytes of a GUID with
/// AES-128 in ECB mode (single block ⇒ no IV needed, deterministic output so the same GUID
/// always yields the same token — required for stable URLs, ETags and link de-duplication),
/// then renders the 16-byte ciphertext as URL-safe Base64 (no padding).
/// <para>
/// Why AES rather than Hashids/Sqids: zero external dependencies, exact 16↔16 byte mapping
/// (token is a fixed 22 chars), and the key keeps tokens non-trivially reversible by clients.
/// A single GUID is exactly one AES block, so ECB leaks nothing here (there is no
/// multi-block pattern to expose) while staying deterministic.
/// </para>
/// <para>
/// Registered as a singleton — the <see cref="Aes"/> instance is created once and reused;
/// <see cref="Aes.EncryptEcb"/>/<see cref="Aes.DecryptEcb"/> are thread-safe one-shot calls.
/// </para>
/// </summary>
public sealed class AesIdEncoder : IIdEncoder
{
    private readonly byte[] _key;

    public AesIdEncoder(IOptions<IdEncoderOptions> options)
    {
        var secret = options.Value.Secret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "IdEncoder:Secret is not configured. Set a stable, non-empty secret " +
                "(user-secrets in dev, Key Vault in prod) so encoded IDs remain valid across restarts.");
        }

        // Derive a fixed 128-bit key from the configured secret (SHA-256 → first 16 bytes).
        // Deterministic: identical secret ⇒ identical key ⇒ stable tokens.
        _key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))[..16];
    }

    public string Encode(Guid id)
    {
        Span<byte> plain = stackalloc byte[16];
        id.TryWriteBytes(plain);

        Span<byte> cipher = stackalloc byte[16];
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.EncryptEcb(plain, cipher, PaddingMode.None);

        return ToBase64Url(cipher);
    }

    public bool TryDecode(string? value, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Backward compatibility: a plain GUID string resolves as-is so existing
        // links/bookmarks keep working during the Admin transition.
        if (Guid.TryParse(value, out var raw))
        {
            id = raw;
            return true;
        }

        if (!TryFromBase64Url(value, out var cipher) || cipher.Length != 16)
            return false;

        try
        {
            Span<byte> plain = stackalloc byte[16];
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.DecryptEcb(cipher, plain, PaddingMode.None);
            id = new Guid(plain);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public Guid? Decode(string? value) => TryDecode(value, out var id) ? id : null;

    // ── URL-safe Base64 (RFC 4648 §5) without padding ────────────────────────────
    private static string ToBase64Url(ReadOnlySpan<byte> bytes)
    {
        var s = Convert.ToBase64String(bytes);
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryFromBase64Url(string value, out byte[] bytes)
    {
        bytes = [];
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
            case 1: return false; // never a valid base64 length
        }

        try
        {
            bytes = Convert.FromBase64String(s);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
