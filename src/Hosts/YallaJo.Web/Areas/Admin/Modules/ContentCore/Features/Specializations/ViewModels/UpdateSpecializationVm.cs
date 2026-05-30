using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.ViewModels;

public sealed class UpdateSpecializationVm
{
    [Required] public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(100)]
    [Display(Name = "Icon")]
    public string? Icon { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
