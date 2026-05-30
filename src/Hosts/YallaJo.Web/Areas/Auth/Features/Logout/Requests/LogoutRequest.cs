namespace YallaJo.Web.Areas.Auth.Features.Logout.Requests;

public sealed class LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
