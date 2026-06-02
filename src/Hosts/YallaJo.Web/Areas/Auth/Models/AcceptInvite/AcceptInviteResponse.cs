namespace YallaJo.Web.Areas.Auth.Models.AcceptInvite;

public sealed class AcceptInviteResponse
{
    public Guid   UserId  { get; init; }
    public string Message { get; init; } = string.Empty;
}
