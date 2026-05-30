namespace ContentTours.Application.Caching;

public static class TourGuideCacheKeys
{
    public static string List(Guid tourId)
        => $"ct:tour-guides:{tourId}";

    public static string Profile(Guid guideId)
        => $"ct:tour-guide-profile:{guideId}";

    // F15 fix: TourGuide profile resolved by owning user identity (GET /api/v1/guides/me).
    public static string ProfileByUser(Guid userId)
        => $"ct:tour-guide-profile:user:{userId}";

    public static string TagForProfileByUser(Guid userId)
        => $"tour-guide-profile:user:{userId}";

    public static string ProfileBySlug(string slug)
        => $"ct:tour-guide-profile:slug:{slug.Trim().ToLowerInvariant()}";

    public static string TagForTour(Guid tourId)
        => $"tour-guides:{tourId}";

    public static string TagForProfile(Guid guideId)
        => $"tour-guide-profile:{guideId}";

    public static string TagForProfileSlug(string slug)
        => $"tour-guide-profile:slug:{slug.Trim().ToLowerInvariant()}";

    // Public Guide List
    public static string AllGuidesList(int page, int pageSize)
        => $"ct:tour-guides:all:p{page}:s{pageSize}";

    public const string TagAllGuidesList = "tour-guides:all";

    public static string GuideTours(Guid guideId, int page, int pageSize)
        => $"ct:tour-guide-tours:{guideId}:p{page}:s{pageSize}";

    public static string TagForGuideTours(Guid guideId)
        => $"tour-guide-tours:{guideId}";

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

    public static string AvailabilityBlocks(Guid guideUserId)
        => $"ct:guide-dashboard:availability-blocks:user:{guideUserId}";

    public static string TagForAvailabilityBlocks(Guid guideUserId)
        => $"guide-dashboard:availability-blocks:user:{guideUserId}";

    public static string TierProgress(Guid guideUserId)
        => $"ct:guide-dashboard:tier:user:{guideUserId}";

    public static string TagForTierProgress(Guid guideUserId)
        => $"guide-dashboard:tier:user:{guideUserId}";

    public static string EarningsSummary(Guid guideUserId)
        => $"ct:guide-dashboard:earnings:summary:user:{guideUserId}";

    public static string EarningsByTour(Guid guideUserId)
        => $"ct:guide-dashboard:earnings:by-tour:user:{guideUserId}";

    public static string EarningsHistory(Guid guideUserId, int page, int pageSize)
        => $"ct:guide-dashboard:earnings:history:user:{guideUserId}:p{page}:s{pageSize}";

    public static string TagForEarnings(Guid guideUserId)
        => $"guide-dashboard:earnings:user:{guideUserId}";

    public static string AnalyticsOverview(Guid guideUserId)
        => $"ct:guide-dashboard:analytics:overview:user:{guideUserId}";

    public static string BookingTrends(Guid guideUserId, string granularity, int months)
        => $"ct:guide-dashboard:analytics:booking-trends:user:{guideUserId}:g:{granularity}:m:{months}";

    public static string PopularTours(Guid guideUserId, int limit)
        => $"ct:guide-dashboard:analytics:popular-tours:user:{guideUserId}:l:{limit}";

    public static string PeakDays(Guid guideUserId)
        => $"ct:guide-dashboard:analytics:peak-days:user:{guideUserId}";

    public static string TagForAnalytics(Guid guideUserId)
        => $"guide-dashboard:analytics:user:{guideUserId}";
}
