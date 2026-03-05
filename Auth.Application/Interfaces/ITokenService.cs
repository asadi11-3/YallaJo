namespace Auth.Application.Interfaces;

/// <summary>
/// JWT access token generation and refresh token utilities.
/// Lives in Auth.Application — NOT in SharedKernel.
/// </summary>
public interface ITokenService
{
    /// <summary>Generates a signed JWT access token from the given identity data.</summary>
    string GenerateAccessToken(TokenData data);

    /// <summary>Generates a cryptographically secure opaque refresh token.</summary>
    string GenerateRefreshToken();

    /// <summary>Hashes a plain refresh token for safe storage in the database.</summary>
    string HashRefreshToken(string plainToken);
}

/// <summary>
/// Data needed to issue a JWT access token.
/// Populated from <see cref="Security.Contracts.Abstractions.SecurityUserData"/>.
/// </summary>
public sealed record TokenData(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> AdditionalClaims,
    Guid? SessionId = null);
