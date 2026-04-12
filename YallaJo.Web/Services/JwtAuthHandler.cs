using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.Claims;

namespace YallaJo.Web.Services;

/// <summary>
/// DelegatingHandler that transparently attaches a Bearer token to every
/// outbound API request made through ApiClient.
///
/// Token lifecycle (per-request):
///  1. Read access token from the encrypted cookie claims.
///  2. Decode the JWT locally (no signature check — just read the exp claim).
///  3. If the token is expired or expiring within 30 seconds, call
///     POST /api/v1/auth/refresh using a plain "anon" HttpClient that
///     does NOT go through this handler (prevents infinite loops).
///  4. On successful refresh: update the cookie so the browser gets the
///     new token on its next request, and also update HttpContext.User
///     in-memory so the rest of this request uses the new token.
///  5. Attach Authorization: Bearer {accessToken} to the outgoing request.
///
/// On a 401 from the backend (edge case: token expired between check and call)
/// we simply return the 401 — the Facade or Controller is responsible for
/// signing the user out and redirecting to login.
/// </summary>
public sealed class JwtAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;
    private readonly IHttpClientFactory   _factory;

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public JwtAuthHandler(IHttpContextAccessor accessor, IHttpClientFactory factory)
    {
        _accessor = accessor;
        _factory  = factory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken  cancellationToken)
    {
        var context = _accessor.HttpContext;
        if (context is null)
            return await base.SendAsync(request, cancellationToken);

        var accessToken = context.User.FindFirstValue(AppClaimTypes.AccessToken);

        // ── Proactive refresh ─────────────────────────────────────────────
        if (accessToken is not null && IsExpiredOrExpiringSoon(accessToken))
        {
            var refreshToken = context.User.FindFirstValue(AppClaimTypes.RefreshToken);
            if (refreshToken is not null)
            {
                var pair = await TryRefreshAsync(refreshToken, cancellationToken);
                if (pair is not null)
                {
                    await UpdateCookieAsync(context, pair);
                    accessToken = pair.AccessToken;
                }
                else
                {
                    // Refresh failed — proceed without token; backend will return 401.
                    accessToken = null;
                }
            }
        }

        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }

    // ── JWT expiry check ─────────────────────────────────────────────────────

    private static bool IsExpiredOrExpiringSoon(string token, int bufferSeconds = 30)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return true;
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo <= DateTime.UtcNow.AddSeconds(bufferSeconds);
        }
        catch
        {
            return true; // If unparseable, treat as expired.
        }
    }

    // ── Token refresh (anonymous client — no JwtAuthHandler) ─────────────────

    private async Task<TokenPair?> TryRefreshAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            var client = _factory.CreateClient("anon");
            var body   = JsonSerializer.Serialize(new { RefreshToken = refreshToken }, WriteOpts);
            using var content  = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/api/v1/auth/refresh", content, ct);

            if (!response.IsSuccessStatusCode) return null;

            var json   = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<TokenPair>(json, ReadOpts);
            return result;
        }
        catch
        {
            return null;
        }
    }

    // ── Cookie update ────────────────────────────────────────────────────────

    private static async Task UpdateCookieAsync(HttpContext context, TokenPair pair)
    {
        // Rebuild the claims list, replacing the token claims.
        var existingClaims = context.User.Claims
            .Where(c => c.Type != AppClaimTypes.AccessToken
                     && c.Type != AppClaimTypes.RefreshToken
                     && c.Type != AppClaimTypes.RefreshTokenExpiresAt)
            .ToList();

        existingClaims.Add(new Claim(AppClaimTypes.AccessToken,           pair.AccessToken));
        existingClaims.Add(new Claim(AppClaimTypes.RefreshToken,          pair.RefreshToken));
        existingClaims.Add(new Claim(AppClaimTypes.RefreshTokenExpiresAt, pair.RefreshTokenExpiresAt.ToString("O")));

        var identity  = new ClaimsIdentity(existingClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Persist to cookie (next browser request will carry the new token).
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Also update the in-memory principal so the rest of THIS request
        // (e.g. a second API call in the same request) uses the new token.
        context.User = principal;
    }

    // ── Internal DTO for deserialization ─────────────────────────────────────

    private sealed record TokenPair(
        string   AccessToken,
        string   RefreshToken,
        DateTime RefreshTokenExpiresAt);
}
