namespace YallaJo.Web.Areas.Admin.Models.Invitations;

public sealed record InviteUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    IReadOnlyList<Guid> RoleIds);
