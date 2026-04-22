using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// HMAC-SHA256 JWT implementation of <see cref="IExternalAuthTicketBuilder"/>.
///
/// <para>
/// Tickets are intentionally short-lived (default 120s) so that even a network
/// capture leaves only a tiny replay window — and the API additionally enforces
/// single-use via its nonce store. Keys come from <see cref="ExternalAuthOptions.SigningKey"/>,
/// which the operator sets through user-secrets or environment variables.
/// </para>
/// </summary>
internal sealed class ExternalAuthTicketBuilder : IExternalAuthTicketBuilder
{
    private static readonly JwtSecurityTokenHandler _handler = new() { MapInboundClaims = false };

    private readonly ExternalAuthOptions _opts;
    private readonly SigningCredentials _credentials;
    private readonly HashSet<string> _allowedProviders;

    public ExternalAuthTicketBuilder(IOptions<ExternalAuthOptions> options)
    {
        _opts = options.Value;

        if (string.IsNullOrWhiteSpace(_opts.SigningKey))
            throw new InvalidOperationException(
                "ExternalAuth:SigningKey is not configured on the Web host. " +
                "Configure a strong shared secret (>=32 bytes) identical to the API's ExternalAuth:SigningKey.");

        var keyBytes = Encoding.UTF8.GetBytes(_opts.SigningKey);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException(
                "ExternalAuth:SigningKey must be at least 32 bytes (UTF-8).");

        var key = new SymmetricSecurityKey(keyBytes);
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _allowedProviders = new HashSet<string>(
            _opts.AllowedProviders.Select(p => p.Trim().ToLowerInvariant()),
            StringComparer.Ordinal);
    }

    public string Build(
        string provider,
        string providerUserId,
        string? email,
        bool emailVerifiedByProvider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));

        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new ArgumentException("Provider user ID is required.", nameof(providerUserId));

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        if (!_allowedProviders.Contains(normalizedProvider))
        {
            throw new InvalidOperationException(
                $"Provider '{provider}' is not configured in ExternalAuth:AllowedProviders.");
        }

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddSeconds(_opts.TicketLifetimeSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new(ExternalAuthTicketClaims.Provider, normalizedProvider),
            new(ExternalAuthTicketClaims.ProviderUserId, providerUserId.Trim()),
            new(ExternalAuthTicketClaims.EmailVerified,
                emailVerifiedByProvider ? "true" : "false", ClaimValueTypes.Boolean),
        };

        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(ExternalAuthTicketClaims.Email, email.Trim().ToLowerInvariant()));

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: _credentials);

        return _handler.WriteToken(token);
    }
}
