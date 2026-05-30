namespace Auth.Application.Commands.Login;

public sealed record LoginResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
