namespace YallaJo.Web.Areas.Provider.Models.TourApplications;

public static class TourApplicationsMapper
{
    public static TourApplicationsIndexVm ToIndexVm(
        Guid tourId, string tourName, string tourStatusLabel,
        ListGuideApplicationsResponse response) => new()
    {
        TourId          = tourId,
        TourName        = tourName,
        TourStatusLabel = tourStatusLabel,
        TotalCount      = response.TotalCount,
        Applications    = response.Items
            .OrderByDescending(a => a.CreatedAt)
            .Select(ToRowVm)
            .ToList(),
    };

    private static TourApplicationRowVm ToRowVm(GuideApplicationResponse a) => new()
    {
        ApplicationId      = a.ApplicationId,
        GuideUserId        = a.GuideUserId,
        Status             = a.Status,
        Message            = a.Message,
        RelevantExperience = a.RelevantExperience,
        ProposedBasePrice  = a.ProposedBasePrice,
        ResubmissionCount  = a.ResubmissionCount,
        CreatedAt          = a.CreatedAt,
        RejectionReason    = a.RejectionReason,
    };
}
