using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.ViewModels;

public sealed class UpdateLanguageVm
{
    [Required]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    [Display(Name = "English name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Native name is required.")]
    [StringLength(100)]
    [Display(Name = "Native name")]
    public string NativeName { get; set; } = string.Empty;

    [Display(Name = "Right-to-left")]
    public bool IsRtl { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
