namespace YallaJo.Web.Areas.Admin.Models.Guides;

public sealed class GuidesVm
{
    public Guid? LookupId { get; set; }
    public TourGuideProfileVm? Detail { get; set; }
}

public sealed class TourGuideProfileVm
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Bio { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public bool HasFirstAid { get; set; }
    public string? MoTALicenseNumber { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int TourCount { get; set; }
    public IReadOnlyList<string> Languages { get; set; } = [];
    public IReadOnlyList<string> Specializations { get; set; } = [];
}
