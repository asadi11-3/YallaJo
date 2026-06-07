using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Guides;

public sealed class GuidesLookupRequest
{
    public Guid? Id { get; set; }
}

/// <summary>
/// §8.14 — admin edit of a tour guide profile. Bound from the Guides page edit form
/// and forwarded to PUT /api/v1/guides/admin/{guideId} as AdminUpdateTourGuideRequest.
/// </summary>
public sealed class AdminEditGuideVm
{
    [Required]
    public Guid Id { get; set; }

    [StringLength(2000)]
    public string? Bio { get; set; }

    [Range(0, 80)]
    public int? YearsOfExperience { get; set; }

    public bool HasFirstAid { get; set; }

    [StringLength(100)]
    public string? MoTALicenseNumber { get; set; }
}

// Body for PUT /api/v1/guides/admin/{guideId} — mirrors the backend contract.
public sealed record AdminUpdateGuideApiRequest(
    string? Bio,
    int? YearsOfExperience,
    bool? HasFirstAid,
    string? MoTALicenseNumber);
