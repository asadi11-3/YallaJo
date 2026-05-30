namespace Auth.Application.Interfaces.SessionRevocation;

public sealed record SessionRevocationOutcome(
    int SessionsRevoked,
    int RefreshTokensRevoked);
