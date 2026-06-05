using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Business.Models.Accessibility;

public sealed class AccessibilityVm
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string Status { get; set; } = "";
    public List<AccessibilityFeatureFormVm> Features { get; set; } = [];
}

public sealed class AccessibilityFeatureFormVm
{
    [Range(0, 5)]
    public AccessibilityFeatureType FeatureType { get; set; }

    [Display(Name = "Feature")]
    public string FeatureLabel { get; set; } = "";

    [Required]
    [StringLength(150, MinimumLength = 1)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Available")]
    public bool IsAvailable { get; set; }
}
