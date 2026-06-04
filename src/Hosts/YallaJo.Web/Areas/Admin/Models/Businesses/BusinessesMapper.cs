namespace YallaJo.Web.Areas.Admin.Models.Businesses;

public static class BusinessesMapper
{
    public static BusinessesVm ToVm(BusinessPageResponse page, Guid? placeId, string? statusFilter)
    {
        IEnumerable<BusinessItemResponse> items = page.Items;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            items = items.Where(b =>
                string.Equals(b.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
        }

        return new BusinessesVm
        {
            PlaceId = placeId,
            StatusFilter = statusFilter,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = page.TotalPages,
            HasNext = page.HasNextPage,
            HasPrevious = page.HasPreviousPage,
            Businesses = items.Select(b => new BusinessRowVm
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                BusinessType = b.BusinessType,
                Status = b.Status,
                City = b.City,
                Country = b.Country,
                AverageRating = b.AverageRating,
                ReviewCount = b.ReviewCount,
                IsVerified = b.IsVerified,
                IsFeatured = b.IsFeatured,
            }).ToList(),
        };
    }

    public static string StatusColor(string status) => status switch
    {
        "Approved" => "success",
        "Pending" => "warning",
        "Rejected" => "danger",
        "Suspended" => "danger",
        "MoreDocsNeeded" => "info",
        _ => "secondary",
    };
}
