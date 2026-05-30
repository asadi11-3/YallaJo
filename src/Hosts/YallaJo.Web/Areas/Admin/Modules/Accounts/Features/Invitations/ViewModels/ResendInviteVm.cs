using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations.ViewModels;

public sealed class ResendInviteVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;
}
