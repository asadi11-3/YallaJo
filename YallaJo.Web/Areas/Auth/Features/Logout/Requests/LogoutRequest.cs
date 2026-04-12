namespace YallaJo.Web.Areas.Auth.Features.Logout.Requests;

/// <summary>Outbound payload for POST /api/v1/auth/logout.</summary>
public sealed class LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
