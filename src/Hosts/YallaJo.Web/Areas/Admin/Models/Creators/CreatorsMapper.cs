namespace YallaJo.Web.Areas.Admin.Models.Creators;

public static class CreatorsMapper
{
    public static CreatorsVm ToVm(
        CreatorApplicationPageResponse page,
        CreatorApplicationDetailResponse? detail,
        string? statusFilter,
        IReadOnlyDictionary<Guid, string>? applicantEmails = null)
    {
        var emails = applicantEmails ?? new Dictionary<Guid, string>();

        var vm = new CreatorsVm
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = page.TotalPages,
            HasNextPage = page.HasNextPage,
            HasPreviousPage = page.HasPreviousPage,
            StatusFilter = statusFilter,
            Applications = page.Items.Select(i => new CreatorApplicationRowVm
            {
                Id = i.Id,
                ApplicantUserId = i.ApplicantUserId,
                ApplicantEmail = emails.TryGetValue(i.ApplicantUserId, out var email) ? email : null,
                Status = i.Status,
                Source = i.Source,
                ReapplicationCount = i.ReapplicationCount,
                CreatedAt = i.CreatedAt,
            }).ToList(),
        };

        if (detail is not null)
        {
            vm.LookupId = detail.Id;
            vm.Detail = new CreatorApplicationDetailVm
            {
                Id = detail.Id,
                ApplicantUserId = detail.ApplicantUserId,
                ApplicantEmail = emails.TryGetValue(detail.ApplicantUserId, out var detailEmail) ? detailEmail : null,
                Bio = detail.Bio,
                Status = detail.Status,
                Source = detail.Source,
                PortfolioUrls = detail.PortfolioUrls,
                SampleWorkUrls = detail.SampleWorkUrls,
                FreeTags = detail.FreeTags,
                SocialHandles = detail.SocialHandles,
                AdminNote = detail.AdminNote,
                ReapplicationCount = detail.ReapplicationCount,
                LastRejectedAt = detail.LastRejectedAt,
                CreatedAt = detail.CreatedAt,
                ReviewedAt = detail.ReviewedAt,
            };
        }

        return vm;
    }

    public static CreatorApplicationStatusCountsVm ToStatusCountsVm(CreatorApplicationStatusCountsResponse r) => new()
    {
        Draft          = r.Draft,
        Pending        = r.Pending,
        Approved       = r.Approved,
        Rejected       = r.Rejected,
        MoreInfoNeeded = r.MoreInfoNeeded,
    };

    public static string StatusColor(string status) => status switch
    {
        "Approved" => "success",
        "Pending" => "warning",
        "Draft" => "secondary",
        "Rejected" => "danger",
        "MoreInfoNeeded" => "info",
        _ => "secondary",
    };

    // A11Y5: status conveyed by colour + icon + text, never colour alone.
    public static string StatusIcon(string status) => status switch
    {
        "Approved" => "circle-check",
        "Pending" => "clock",
        "Draft" => "pen-ruler",
        "Rejected" => "circle-xmark",
        "MoreInfoNeeded" => "circle-question",
        _ => "circle-info",
    };
}
