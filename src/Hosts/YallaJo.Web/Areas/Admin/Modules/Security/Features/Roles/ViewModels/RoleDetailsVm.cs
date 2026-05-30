namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;

public sealed class RoleDetailsVm
{
    public Guid    RoleId      { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool    IsActive    { get; init; }
    public UpdateRoleVm    UpdateVm  { get; init; } = new();
    public AddRoleClaimVm  AddClaim  { get; init; } = new();
    public IReadOnlyList<RoleClaimVm> Claims { get; init; } = [];
}
