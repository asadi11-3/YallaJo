using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Invitations;

public sealed class InviteUserVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    [StringLength(2048)]
    [Url(ErrorMessage = "Avatar URL must be a valid URL.")]
    [Display(Name = "Avatar URL")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "Initial roles")]
    [MinLength(1, ErrorMessage = "Select at least one initial role.")]
    public List<Guid> SelectedRoleIds { get; set; } = [];

    public IReadOnlyList<InvitableRoleOptionVm> AvailableRoles { get; set; } = [];
}
