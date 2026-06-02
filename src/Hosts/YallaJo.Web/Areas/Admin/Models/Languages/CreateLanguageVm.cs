using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed class CreateLanguageVm
{
    [Required(ErrorMessage = "Code is required.")]
    [StringLength(10, ErrorMessage = "Code must be 10 characters or fewer.")]
    [Display(Name = "Code (e.g. en, ar)")]
    public string Code { get; set; } = string.Empty;

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
}
