namespace Auth.Application.Interfaces;


public interface ITokenService
{
    string GenerateAccessToken(TokenData data);
    string GenerateRefreshToken();
    string HashRefreshToken(string plainToken);
}


public sealed record TokenData(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> AdditionalClaims,
    Guid? SessionId = null);
