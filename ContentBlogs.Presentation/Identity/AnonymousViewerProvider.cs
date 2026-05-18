using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace ContentBlogs.Presentation.Identity;

public sealed class AnonymousViewerProvider(IHttpContextAccessor httpAccessor)
{
    internal const string CookieName = "yj_av";

    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(365);

    public string GetOrCreate()
    {
        var ctx = httpAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "AnonymousViewerProvider requires an active HttpContext.");

        if (ctx.Request.Cookies.TryGetValue(CookieName, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var token = GenerateToken();

        ctx.Response.Cookies.Append(CookieName, token, BuildCookieOptions(ctx));

        return token;
    }

    private static string GenerateToken()
    {
        Span<byte> buffer = stackalloc byte[16];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    internal static CookieOptions BuildCookieOptions(HttpContext ctx) => new()
    {
        HttpOnly    = true,
        Secure      = IsSecureHost(ctx),
        SameSite    = SameSiteMode.Lax,
        Expires     = DateTimeOffset.UtcNow.Add(CookieLifetime),
        IsEssential = true,
    };

    private static bool IsSecureHost(HttpContext ctx) =>
        !string.Equals(ctx.Request.Host.Host, "localhost", StringComparison.OrdinalIgnoreCase);
}
