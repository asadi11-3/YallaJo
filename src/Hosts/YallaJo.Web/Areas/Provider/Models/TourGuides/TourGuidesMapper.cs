namespace YallaJo.Web.Areas.Provider.Models.TourGuides;

public static class TourGuidesMapper
{
    public static TourGuidesIndexVm ToIndexVm(
        Guid tourId, string tourName, string tourStatusLabel,
        IReadOnlyList<TourGuideResponse> guides) => new()
    {
        TourId          = tourId,
        TourName        = tourName,
        TourStatusLabel = tourStatusLabel,
        Guides          = guides
            .OrderByDescending(g => g.IsPrimary)
            .ThenBy(g => g.DisplayName)
            .Select(ToRowVm)
            .ToList(),
    };

    private static TourGuideRowVm ToRowVm(TourGuideResponse g) => new()
    {
        TourGuideId = g.TourGuideId,
        IsPrimary   = g.IsPrimary,
        DisplayName = g.DisplayName,
        AvatarUrl   = g.AvatarUrl,
    };

    public static AssignTourGuideApiRequest ToAssignRequest(AssignTourGuideFormVm vm) =>
        new(vm.TourGuideUserId, vm.IsPrimary);
}
