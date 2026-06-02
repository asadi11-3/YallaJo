using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Places;

public sealed class EditPlaceVm : PlaceFormVm
{
    [Required] public Guid Id { get; set; }

    // Re-declared so DataAnnotations adds the [Required] rule on top of the base regex check.
    [Required(ErrorMessage = "Slug is required.")]
    [StringLength(300)]
    [RegularExpression(@"^[a-z0-9\-]+$",
        ErrorMessage = "Slug must contain only lowercase letters, digits, and hyphens.")]
    [Display(Name = "Slug")]
    public new string Slug { get; set; } = string.Empty;
}
