namespace Auth.Presentation.Endpoints.ExternalProvider.Models;

public sealed record ExternalLoginResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
