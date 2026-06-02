using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Users
{
    public sealed class AssignRoleVm
    {
        [Required(ErrorMessage = "Select a role.")]
        public Guid? RoleId { get; set; }
    }
}
