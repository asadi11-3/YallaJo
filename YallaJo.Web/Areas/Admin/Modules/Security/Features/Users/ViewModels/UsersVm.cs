using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels;

/// <summary>Paginated user list view model.</summary>
public sealed class UserListVm
{
    public IReadOnlyList<UserRowVm> Users    { get; init; } = [];
    public int  Page        { get; init; } = 1;
    public int  PageSize    { get; init; } = 20;
    public int  TotalCount  { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext     { get; init; }
}

public sealed class UserRowVm
{
    public Guid   Id       { get; init; }
    public string Email    { get; init; } = string.Empty;
    public bool   IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

/// <summary>User detail + role/claim management.</summary>
public sealed class UserDetailsVm
{
    public Guid   UserId   { get; init; }
    public string Email    { get; init; } = string.Empty;
    public bool   IsActive { get; init; }
    public IReadOnlyList<string>      CurrentRoles   { get; init; } = [];
    public IReadOnlyList<RoleOptionVm> AvailableRoles { get; init; } = [];
    public AssignRoleVm AssignRole { get; init; } = new();
    public AddClaimVm   AddClaim   { get; init; } = new();
}

public sealed class RoleOptionVm
{
    public Guid   Id   { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class AssignRoleVm
{
    [Required(ErrorMessage = "Select a role.")]
    public Guid RoleId { get; set; }
}

public sealed class AddClaimVm
{
    [Required(ErrorMessage = "Claim type is required.")]
    public string ClaimType  { get; set; } = string.Empty;

    [Required(ErrorMessage = "Claim value is required.")]
    public string ClaimValue { get; set; } = string.Empty;
}
