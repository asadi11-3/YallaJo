using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Tags;

public sealed class CreateTagVm
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(100)]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;
}
