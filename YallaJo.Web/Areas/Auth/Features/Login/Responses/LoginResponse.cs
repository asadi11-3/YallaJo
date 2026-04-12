namespace YallaJo.Web.Areas.Auth.Features.Login.Responses;

/// <summary>Inbound payload from POST /api/v1/auth/login (200 OK).</summary>
public sealed class LoginResponse
{
    public Guid     UserId                 { get; init; }
    public string   AccessToken            { get; init; } = string.Empty;
    public string   RefreshToken           { get; init; } = string.Empty;
    public DateTime RefreshTokenExpiresAt  { get; init; }
}
