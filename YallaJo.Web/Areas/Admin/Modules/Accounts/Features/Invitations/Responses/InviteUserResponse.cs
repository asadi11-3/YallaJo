namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations.Responses;

public sealed class InviteUserResponse
{
    public Guid   UserId    { get; init; }
    public Guid   ProfileId { get; init; }
    public string Message   { get; init; } = string.Empty;
}
