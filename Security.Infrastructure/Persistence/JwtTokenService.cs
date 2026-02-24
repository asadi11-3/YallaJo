using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Interfaces;

namespace Security.Infrastructure.Persistence;

internal sealed class JwtTokenService : IJwtTokenService
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

    public string GenerateAccessToken(UserTokenData data)
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
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var extra in data.AdditionalClaims)
            claims.Add(new Claim(extra.Type, extra.Value));

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.UtcDateTime.AddMinutes(_opts.AccessTokenMinutes),
            signingCredentials: _credentials);

        return _handler.WriteToken(token);
    }
}
