namespace ContentTours.Application.Caching;

public static class TourGuideCacheKeys
{
    public static string List(Guid tourId)
        => $"ct:tour-guides:{tourId}";

    public static string Profile(Guid guideId)
        => $"ct:tour-guide-profile:{guideId}";

    public static string ProfileBySlug(string slug)
        => $"ct:tour-guide-profile:slug:{slug.Trim().ToLowerInvariant()}";

    public static string TagForTour(Guid tourId)
        => $"tour-guides:{tourId}";

    public static string TagForProfile(Guid guideId)
        => $"tour-guide-profile:{guideId}";

    public static string TagForProfileSlug(string slug)
        => $"tour-guide-profile:slug:{slug.Trim().ToLowerInvariant()}";

    // Guide Applications
    public const string TagGuideApplications = "guide-applications";

    public static string TagForTourApplications(Guid tourId)
        => $"guide-applications:tour:{tourId}";

    public static string TagForGuideApplications(Guid tourGuideId)
        => $"guide-applications:guide:{tourGuideId}";

    public static string TourApplications(Guid tourId, int page, int pageSize)
        => $"ct:guide-apps:tour:{tourId}:p{page}:s{pageSize}";

    public static string GuideApplications(Guid tourGuideId, int page, int pageSize)
        => $"ct:guide-apps:guide:{tourGuideId}:p{page}:s{pageSize}";

    // Tour Proposals
    public const string TagTourProposals = "tour-proposals";

    public static string TagForGuideProposals(Guid tourGuideId)
        => $"tour-proposals:guide:{tourGuideId}";

    public static string GuideProposals(Guid tourGuideId, int page, int pageSize)
        => $"ct:tour-proposals:guide:{tourGuideId}:p{page}:s{pageSize}";

    public const string TagAdminProposalQueue = "tour-proposals:admin-queue";

    public static string AdminProposalQueue(int page, int pageSize)
        => $"ct:tour-proposals:admin:p{page}:s{pageSize}";

    // Guide Offerings
    public static string TagForTourOfferings(Guid tourId)
        => $"guide-offerings:tour:{tourId}";

    public static string TagForGuideOfferings(Guid tourGuideId)
        => $"guide-offerings:guide:{tourGuideId}";
}
