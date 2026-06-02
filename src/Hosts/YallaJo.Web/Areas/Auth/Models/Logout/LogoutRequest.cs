namespace YallaJo.Web.Areas.Auth.Models.Logout;

public sealed class LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
