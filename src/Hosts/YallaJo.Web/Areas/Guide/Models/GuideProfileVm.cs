using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models;

public sealed class GuideProfileVm
{
    public bool IsGuide { get; set; }
    public Guid GuideId { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int TourCount { get; set; }
    public IReadOnlyList<string> Languages { get; set; } = [];
    public IReadOnlyList<string> Specializations { get; set; } = [];
    public EditGuideProfileVm Edit { get; set; } = new();
}

public sealed class EditGuideProfileVm
{
    [StringLength(2000)]
    [Display(Name = "Bio")]
    public string Bio { get; set; } = string.Empty;

    [Range(0, 80)]
    [Display(Name = "Years of experience")]
    public int YearsOfExperience { get; set; }

    [Display(Name = "First aid certified")]
    public bool HasFirstAid { get; set; }

    [StringLength(100)]
    [Display(Name = "MoTA license number")]
    public string? MoTALicenseNumber { get; set; }
}
