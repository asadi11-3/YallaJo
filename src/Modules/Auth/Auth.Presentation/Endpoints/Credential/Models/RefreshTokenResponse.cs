namespace Auth.Presentation.Endpoints.Credential.Models;

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
