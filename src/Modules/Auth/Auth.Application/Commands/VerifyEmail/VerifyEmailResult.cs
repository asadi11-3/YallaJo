namespace Auth.Application.Commands.VerifyEmail;

public sealed record VerifyEmailResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
