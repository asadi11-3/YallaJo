using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Roles;

public sealed class RoleListVm
{
    public IReadOnlyList<RoleRowVm> Roles  { get; init; } = [];
    public CreateRoleVm             Create { get; init; } = new();
}
