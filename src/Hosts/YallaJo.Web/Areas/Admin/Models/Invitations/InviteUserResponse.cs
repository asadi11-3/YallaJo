namespace YallaJo.Web.Areas.Admin.Models.Invitations;

public sealed class InviteUserResponse
{
    public Guid   UserId    { get; init; }
    public Guid   ProfileId { get; init; }
    public string Message   { get; init; } = string.Empty;
}
