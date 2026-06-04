namespace YallaJo.Web.Areas.Admin.Models.Creators;

public static class CreatorsMapper
{
    public static CreatorsVm ToVm(
        CreatorApplicationPageResponse page,
        CreatorApplicationDetailResponse? detail,
        string? statusFilter)
    {
        var vm = new CreatorsVm
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = page.TotalPages,
            HasNextPage = page.HasNextPage,
            HasPreviousPage = page.HasPreviousPage,
            StatusFilter = statusFilter,
            Applications = page.Items.Select(static i => new CreatorApplicationRowVm
            {
                Id = i.Id,
                ApplicantUserId = i.ApplicantUserId,
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

    public static string StatusColor(string status) => status switch
    {
        "Approved" => "success",
        "Pending" => "warning",
        "Draft" => "secondary",
        "Rejected" => "danger",
        "MoreInfoNeeded" => "info",
        _ => "secondary",
    };
}
