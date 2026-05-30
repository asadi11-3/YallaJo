namespace YallaJo.Web.Areas.Auth.Features.AcceptInvite.Responses;

public sealed class AcceptInviteResponse
{
    public Guid   UserId  { get; init; }
    public string Message { get; init; } = string.Empty;
}
