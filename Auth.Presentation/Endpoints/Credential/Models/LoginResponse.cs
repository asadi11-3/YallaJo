namespace Auth.Presentation.Endpoints.Credential.Models;

public sealed record LoginResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
