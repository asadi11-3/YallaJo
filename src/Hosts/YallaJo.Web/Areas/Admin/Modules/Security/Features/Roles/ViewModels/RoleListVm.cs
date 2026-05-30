using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;

public sealed class RoleListVm
{
    public IReadOnlyList<RoleRowVm> Roles  { get; init; } = [];
    public CreateRoleVm             Create { get; init; } = new();
}
