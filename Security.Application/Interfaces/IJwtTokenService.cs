namespace Security.Application.Interfaces;

public sealed record ClaimEntry(string Type, string Value);

public sealed record UserTokenData(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<ClaimEntry> AdditionalClaims);

public interface IJwtTokenService
{
    string GenerateAccessToken(UserTokenData data);
}
