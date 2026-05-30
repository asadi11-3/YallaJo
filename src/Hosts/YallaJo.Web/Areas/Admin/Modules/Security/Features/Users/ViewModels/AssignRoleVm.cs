using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels
{
    public sealed class AssignRoleVm
    {
        [Required(ErrorMessage = "Select a role.")]
        public Guid? RoleId { get; set; }
    }
}
