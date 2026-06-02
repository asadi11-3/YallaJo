using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.Models.Guides;

public static class GuidesMapper
{
    private const int TeamImageCount = 10;

    public static GuideCardVm ToCardVm(
        TourGuideListItemResponse r,
        IReadOnlyDictionary<Guid, string> specializationNames,
        IApiAssetUrlResolver resolver)
    {
        var (url, _) = ResolveGuideImage(r.Id, r.AvatarUrl, resolver);
        var names = r.Specializations
            .Select(s => specializationNames.TryGetValue(s.SpecializationId, out var n) ? n : null)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .ToList();

        return new GuideCardVm
        {
            Id                  = r.Id,
            Slug                = r.Slug ?? string.Empty,
            DisplayName         = string.IsNullOrWhiteSpace(r.DisplayName) ? "Tour Guide" : r.DisplayName!,
            Bio                 = r.Bio,
            AverageRating       = r.AverageRating,
            ReviewCount         = r.ReviewCount,
            TourCount           = r.TourCount,
            SpecializationNames = names,
            GuideImageUrl       = url,
            GuideImageAlt       = r.DisplayName ?? "Tour Guide",
            HasGuideImage       = true,
        };
    }

    public static GuideDetailsVm ToDetailsVm(
        TourGuideProfileResponse r,
        string slug,
        IReadOnlyList<SpecializationVm> specializations,
        IReadOnlyList<GuideTourVm> tours,
        IApiAssetUrlResolver resolver)
    {
        var (url, _) = ResolveGuideImage(r.Id, r.AvatarUrl, resolver);
        return new GuideDetailsVm
        {
            Id                = r.Id,
            Slug              = slug,
            DisplayName       = string.IsNullOrWhiteSpace(r.DisplayName) ? "Tour Guide" : r.DisplayName!,
            Bio               = r.Bio,
            YearsOfExperience = r.YearsOfExperience,
            HasFirstAid       = r.HasFirstAid,
            MoTALicenseNumber = r.MoTALicenseNumber,
            AverageRating     = r.AverageRating,
            ReviewCount       = r.ReviewCount,
            TourCount         = r.TourCount,
            GuideImageUrl     = url,
            GuideImageAlt     = r.DisplayName ?? "Tour Guide",
            HasGuideImage     = true,
            Specializations   = specializations,
            Tours             = tours,
        };
    }

    public static GuideTourVm ToTourVm(GuideTourListItemResponse r) => new()
    {
        TourId            = r.TourId,
        Title             = r.Title,
        Slug              = r.Slug,
        OfferingStatus    = r.OfferingStatus,
        OffersPrivateTour = r.OffersPrivateTour,
    };

    public static SpecializationVm ToSpecializationVm(SpecializationResponse r) => new()
    {
        Id   = r.Id,
        Name = r.Name,
        Icon = r.Icon,
    };

    // ── Image resolution ────────────────────────────────────────────────────────
    // Real avatar first (made absolute via the resolver); placeholder otherwise.
    private static (string Url, bool FromAvatar) ResolveGuideImage(
        Guid id, string? avatarUrl, IApiAssetUrlResolver resolver)
    {
        var resolved = resolver.Resolve(avatarUrl);
        if (!string.IsNullOrWhiteSpace(resolved))
            return (resolved!, true);

        var index = (PositiveHash(id) % TeamImageCount) + 1;
        return ($"/assets/images/team/{index:00}.jpg", false);
    }

    private static int PositiveHash(Guid id)
    {
        var h = id.GetHashCode();
        return h == int.MinValue ? 0 : Math.Abs(h);
    }
}
