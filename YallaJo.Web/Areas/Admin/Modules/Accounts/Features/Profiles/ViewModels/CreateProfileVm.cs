using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.ViewModels;

public sealed class CreateProfileVm
{
    [Required(ErrorMessage = "User ID is required.")]
    [Display(Name = "User ID")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name must be 100 characters or fewer.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name must be 100 characters or fewer.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Display name must be 200 characters or fewer.")]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    [StringLength(2048, ErrorMessage = "Avatar URL must be 2048 characters or fewer.")]
    [Url(ErrorMessage = "Avatar URL must be a valid URL.")]
    [Display(Name = "Avatar URL")]
    public string? AvatarUrl { get; set; }
}
