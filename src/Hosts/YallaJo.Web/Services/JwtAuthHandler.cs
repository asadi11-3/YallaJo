using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.Claims;
using YallaJo.Web.Infrastructure.Authentication.SignIn;

namespace YallaJo.Web.Services;

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

    private static async Task UpdateCookieAsync(HttpContext context, TokenPair pair)
    {
        // Rebuild the claims list.
        // Remove token claims AND the old role/Permission claims so that the new JWT
        // can be the single source of truth for the user's current permissions.
        var existingClaims = context.User.Claims
            .Where(c => c.Type != AppClaimTypes.AccessToken
                     && c.Type != AppClaimTypes.RefreshToken
                     && c.Type != AppClaimTypes.RefreshTokenExpiresAt
                     && c.Type != AppClaimTypes.Role
                     && c.Type != AppClaimTypes.Permission)
            .ToList();

        existingClaims.Add(new Claim(AppClaimTypes.AccessToken,           pair.AccessToken));
        existingClaims.Add(new Claim(AppClaimTypes.RefreshToken,          pair.RefreshToken));
        existingClaims.Add(new Claim(AppClaimTypes.RefreshTokenExpiresAt, pair.RefreshTokenExpiresAt.ToString("O")));

        // Re-extract role + Permission claims from the new access token
        // so any permission changes (e.g. role reassignment) take effect on the next refresh.
        existingClaims.AddRange(
            WebSignInService.ExtractUserClaimsFromJwt(pair.AccessToken));

        var identity  = new ClaimsIdentity(existingClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var props = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var authProps = props?.Properties ?? new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8),
        };

        // Persist to cookie (next browser request will carry the new token).
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        // Also update the in-memory principal so the rest of THIS request
        // (e.g. a second API call in the same request) uses the new token.
        context.User = principal;
    }

    private sealed record TokenPair(
        string   AccessToken,
        string   RefreshToken,
        DateTime RefreshTokenExpiresAt);
}
