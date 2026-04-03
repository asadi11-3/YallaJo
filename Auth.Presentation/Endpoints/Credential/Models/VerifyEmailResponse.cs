namespace Auth.Presentation.Endpoints.Credential.Models;

public sealed record VerifyEmailResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
