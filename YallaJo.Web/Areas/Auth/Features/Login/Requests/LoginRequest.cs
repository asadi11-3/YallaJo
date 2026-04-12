namespace YallaJo.Web.Areas.Auth.Features.Login.Requests;

/// <summary>Outbound payload for POST /api/v1/auth/login.</summary>
public sealed class LoginRequest
{
    public string Email    { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
