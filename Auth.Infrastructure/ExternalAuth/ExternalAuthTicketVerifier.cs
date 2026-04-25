using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Auth.Application.Interfaces.ExternalAuth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Infrastructure.ExternalAuth;

internal sealed class ExternalAuthTicketVerifier : IExternalAuthTicketVerifier
{
    private static readonly JwtSecurityTokenHandler _handler = new() { MapInboundClaims = false };
    private static readonly TimeSpan _clockSkew = TimeSpan.FromSeconds(30);

    private readonly ExternalAuthOptions _opts;
    private readonly TokenValidationParameters _validationParameters;
    private readonly HashSet<string> _allowedProviders;

    public ExternalAuthTicketVerifier(IOptions<ExternalAuthOptions> options)
    {
        _opts = options.Value;

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _opts.Issuer,
            ValidateAudience = true,
            ValidAudience = _opts.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = _clockSkew,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            RequireExpirationTime = true,
            RequireSignedTokens = true,
        };

        _allowedProviders = new HashSet<string>(
            _opts.AllowedProviders.Select(p => p.Trim().ToLowerInvariant()),
            StringComparer.Ordinal);
    }

    public Result<ExternalAuthTicket> Verify(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return Invalid("External provider ticket is missing.");

        if (!_handler.CanReadToken(ticket))
            return Invalid("External provider ticket is malformed.");

        try
        {
            var principal = _handler.ValidateToken(ticket, _validationParameters, out var validated);

            if (validated is not JwtSecurityToken jwt)
                return Invalid("External provider ticket is malformed.");

            // Defense in depth: JwtSecurityTokenHandler already pins alg through
            // ValidAlgorithms, but double-check in case a future refactor changes it.
            if (!string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                return Invalid("External provider ticket uses an unsupported algorithm.");

            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrWhiteSpace(jti) || !Guid.TryParse(jti, out var ticketId))
                return Invalid("External provider ticket is missing a valid nonce.");

            var provider = principal.FindFirst(ExternalAuthTicketClaims.Provider)?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(provider))
                return Invalid("External provider ticket is missing provider.");

            var normalizedProvider = provider.ToLowerInvariant();
            if (!_allowedProviders.Contains(normalizedProvider))
                return Invalid($"Provider '{provider}' is not permitted.");

            var providerUserId = principal.FindFirst(ExternalAuthTicketClaims.ProviderUserId)?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(providerUserId))
                return Invalid("External provider ticket is missing provider_user_id.");

            var email = principal.FindFirst(ExternalAuthTicketClaims.Email)?.Value?.Trim();
            var emailVerified = bool.TryParse(
                principal.FindFirst(ExternalAuthTicketClaims.EmailVerified)?.Value,
                out var ev) && ev;

            var firstName = principal.FindFirst(ExternalAuthTicketClaims.GivenName)?.Value?.Trim();
            var lastName = principal.FindFirst(ExternalAuthTicketClaims.FamilyName)?.Value?.Trim();

            // Reject tickets whose nominal TTL exceeds the configured cap.
            var issuedAt = jwt.IssuedAt == DateTime.MinValue ? jwt.ValidFrom : jwt.IssuedAt;
            var expiresAt = jwt.ValidTo;
            var maxLifetime = TimeSpan.FromSeconds(_opts.TicketLifetimeSeconds) + _clockSkew;
            if (expiresAt - issuedAt > maxLifetime)
                return Invalid("External provider ticket lifetime exceeds the permitted maximum.");

            return Result<ExternalAuthTicket>.Success(new ExternalAuthTicket(
                TicketId: ticketId,
                Provider: normalizedProvider,
                ProviderUserId: providerUserId,
                Email: string.IsNullOrWhiteSpace(email) ? null : email.ToLowerInvariant(),
                EmailVerifiedByProvider: emailVerified,
                IssuedAt: issuedAt,
                ExpiresAt: expiresAt,
                FirstName: string.IsNullOrWhiteSpace(firstName) ? null : firstName,
                LastName: string.IsNullOrWhiteSpace(lastName) ? null : lastName));
        }
        catch (SecurityTokenExpiredException)
        {
            return Invalid("External provider ticket has expired.");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Invalid("External provider ticket signature is invalid.");
        }
        catch (SecurityTokenException ex)
        {
            return Invalid($"External provider ticket is invalid: {ex.Message}");
        }
        catch (CryptographicException)
        {
            return Invalid("External provider ticket signature is invalid.");
        }
    }

    private static Result<ExternalAuthTicket> Invalid(string message) =>
        Result<ExternalAuthTicket>.Failure(
            Error.Unauthorized(message),
            Outcome.Unauthorized);
}
