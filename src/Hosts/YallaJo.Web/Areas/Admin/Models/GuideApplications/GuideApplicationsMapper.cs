namespace YallaJo.Web.Areas.Admin.Models.GuideApplications;

public static class GuideApplicationsMapper
{
    public static GuideApplicationsVm ToVm(
        GuideApplicationPageResponse page,
        Guid? tourId,
        string? statusFilter,
        int currentPage,
        int pageSize,
        IReadOnlyDictionary<Guid, string>? guideEmails = null)
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
                GuideEmail = guideEmails is not null && guideEmails.TryGetValue(item.GuideUserId, out var email)
                    ? email
                    : null,
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

    public static string StatusIcon(string status) => status switch
    {
        "Approved" => "circle-check",
        "Submitted" => "clock",
        "Rejected" => "circle-xmark",
        "Draft" => "pen-ruler",
        _ => "circle-info",
    };
}
