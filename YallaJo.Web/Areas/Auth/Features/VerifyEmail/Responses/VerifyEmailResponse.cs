namespace YallaJo.Web.Areas.Auth.Features.VerifyEmail.Responses;

/// <summary>Inbound payload from POST /api/v1/auth/verify-email (200 OK).</summary>
public sealed class VerifyEmailResponse
{
    public Guid     UserId                { get; init; }
    public string   AccessToken           { get; init; } = string.Empty;
    public string   RefreshToken          { get; init; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; init; }
}
