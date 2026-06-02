using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Roles
{
    public sealed class AddRoleClaimVm
    {
        [Required(ErrorMessage = "Claim type is required.")]
        public string ClaimType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Claim value is required.")]
        public string ClaimValue { get; set; } = string.Empty;
    }
}
