using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Application.Interfaces;
using Auth.Infrastructure.Configures;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Services;

internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opts;
    private readonly SigningCredentials _credentials;
    private readonly JwtSecurityTokenHandler _handler;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _opts = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _handler = new JwtSecurityTokenHandler();
    }

    public string GenerateAccessToken(TokenData data)
    {
        var now = DateTimeOffset.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, data.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, data.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        foreach (var role in data.Roles)
            claims.Add(new Claim("role", role));

        foreach (var (type, value) in data.AdditionalClaims)
            claims.Add(new Claim(type, value));

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.UtcDateTime.AddMinutes(_opts.AccessTokenMinutes),
            signingCredentials: _credentials);

        return _handler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToBase64String(bytes);
    }
}
