namespace YallaJo.Web.Areas.Auth.Features.AcceptInvite.Requests;

public sealed class AcceptInviteRequest
{
    public string Email           { get; init; } = string.Empty;
    public string Token           { get; init; } = string.Empty;
    public string Password        { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}
