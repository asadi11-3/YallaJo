using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.TourGuides;

public sealed class TourGuidesIndexVm
{
    public Guid TourId { get; init; }
    public string TourName { get; init; } = string.Empty;
    public string TourStatusLabel { get; init; } = string.Empty;

    public List<TourGuideRowVm> Guides { get; init; } = [];
    public AssignTourGuideFormVm Assign { get; init; } = new();

    public bool HasGuides => Guides.Count > 0;
}

public sealed class TourGuideRowVm
{
    public Guid TourGuideId { get; init; }
    public bool IsPrimary { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
}

public sealed class AssignTourGuideFormVm
{
    [Required(ErrorMessage = "Select a guide to assign.")]
    [Display(Name = "Guide user ID")]
    public Guid TourGuideUserId { get; set; }

    [Display(Name = "Make primary guide")]
    public bool IsPrimary { get; set; }
}
