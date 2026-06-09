namespace YallaJo.Web.Areas.Admin.Models.Guides;

public sealed class GuidesVm
{
    public Guid? LookupId { get; set; }
    public TourGuideProfileVm? Detail { get; set; }

    /// <summary>Guide-name options for the lookup picker (F10 — no raw GUID input).</summary>
    public IReadOnlyList<GuideOptionVm> GuideOptions { get; set; } = [];
}

/// <summary>A selectable guide (human name shown, id submitted) for the lookup picker.</summary>
public sealed class GuideOptionVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class TourGuideProfileVm
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Resolved account email for the guide's user (F10 — shown instead of the raw UserId GUID); null when unresolvable.</summary>
    public string? UserEmail { get; set; }

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
