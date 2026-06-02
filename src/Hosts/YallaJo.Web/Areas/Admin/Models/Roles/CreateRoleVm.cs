using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Roles
{
    public sealed class CreateRoleVm
    {
        [Required(ErrorMessage = "Role name is required.")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

}
