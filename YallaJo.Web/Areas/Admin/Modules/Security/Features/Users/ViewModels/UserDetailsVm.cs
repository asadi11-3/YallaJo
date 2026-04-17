namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels;

public sealed class UserDetailsVm
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<string> CurrentRoles { get; init; } = [];
    public IReadOnlyList<RoleOptionVm> AvailableRoles { get; init; } = [];
    public IReadOnlyList<UserClaimVm> Claims { get; init; } = [];
}
