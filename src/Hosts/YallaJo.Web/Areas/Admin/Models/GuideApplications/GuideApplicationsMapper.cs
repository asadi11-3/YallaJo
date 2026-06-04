namespace YallaJo.Web.Areas.Admin.Models.GuideApplications;

public static class GuideApplicationsMapper
{
    public static GuideApplicationsVm ToVm(
        GuideApplicationPageResponse page,
        Guid? tourId,
        string? statusFilter,
        int currentPage,
        int pageSize)
    {
        var rows = page.Items
            .Select(item => new GuideApplicationRowVm
            {
                ApplicationId = item.ApplicationId,
                TourId = item.TourId,
                TourGuideId = item.TourGuideId,
                GuideUserId = item.GuideUserId,
                TourTitle = item.TourTitle,
                Status = item.Status,
                Message = item.Message,
                RelevantExperience = item.RelevantExperience,
                ProposedBasePrice = item.ProposedBasePrice,
                ResubmissionCount = item.ResubmissionCount,
                CreatedAt = item.CreatedAt,
                ReviewedAt = item.ReviewedAt,
                RejectionReason = item.RejectionReason,
            })
            .ToList();

        return new GuideApplicationsVm
        {
            TourId = tourId,
            StatusFilter = statusFilter,
            Applications = rows,
            TotalCount = page.TotalCount,
            Page = currentPage,
            PageSize = pageSize,
            HasPrevious = currentPage > 1,
            HasNext = (long)currentPage * pageSize < page.TotalCount,
        };
    }

    public static string StatusColor(string status) => status switch
    {
        "Approved" => "success",
        "Submitted" => "warning",
        "Rejected" => "danger",
        "Draft" => "secondary",
        _ => "secondary",
    };
}
