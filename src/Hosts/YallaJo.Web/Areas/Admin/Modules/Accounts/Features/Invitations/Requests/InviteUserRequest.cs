namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations.Requests;

public sealed record InviteUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    IReadOnlyList<Guid> RoleIds);
