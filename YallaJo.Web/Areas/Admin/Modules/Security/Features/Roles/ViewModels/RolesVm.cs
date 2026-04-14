using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;

/// <summary>
/// Role list page data. Carries domain data only — no permission flags.
/// Action-level UI visibility is controlled in the view by &lt;permission require="..."&gt; TagHelpers.
/// </summary>
public sealed class RoleListVm
{
    public IReadOnlyList<RoleRowVm> Roles  { get; init; } = [];
    public CreateRoleVm             Create { get; init; } = new();
}

public sealed class RoleRowVm
{
    public Guid    Id          { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool    IsActive    { get; init; }
}

public sealed class CreateRoleVm
{
    [Required(ErrorMessage = "Role name is required.")]
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Role detail — update description + manage claims.</summary>
public sealed class RoleDetailsVm
{
    public Guid    RoleId      { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool    IsActive    { get; init; }
    public UpdateRoleVm UpdateVm { get; init; } = new();
    public AddRoleClaimVm AddClaim { get; init; } = new();
}

public sealed class UpdateRoleVm
{
    public string? Description { get; set; }
}

public sealed class AddRoleClaimVm
{
    [Required(ErrorMessage = "Claim type is required.")]
    public string ClaimType  { get; set; } = string.Empty;

    [Required(ErrorMessage = "Claim value is required.")]
    public string ClaimValue { get; set; } = string.Empty;
}
