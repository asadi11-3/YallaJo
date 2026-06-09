namespace YallaJo.Web.Areas.Admin.Models.Trips;

/// <summary>Maps Tour API responses to the Trips view model.</summary>
public static class TripsMapper
{
    public static TripsVm ToVm(TourPageResponse page, TourDetailResponse? detail)
    {
        var vm = new TripsVm
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = page.TotalPages,
            HasNextPage = page.HasNextPage,
            HasPreviousPage = page.HasPreviousPage,
            Tours = page.Items
                .Select(t => new TourRowVm
                {
                    Id = t.Id,
                    Name = t.Name,
                    BasePrice = t.BasePrice,
                    Currency = t.Currency,
                    Status = t.Status,
                    AverageRating = t.AverageRating,
                    BookingCount = t.BookingCount,
                    CreatedAt = t.CreatedAt,
                })
                .ToList(),
        };

        if (detail is not null)
        {
            vm.LookupId = detail.Id;
            vm.Detail = new TourDetailVm
            {
                Id = detail.Id,
                Name = detail.Name,
                Slug = detail.Slug,
                ShortDescription = detail.ShortDescription,
                BasePrice = detail.BasePrice,
                Currency = detail.Currency,
                SalePrice = detail.SalePrice,
                DurationMinutes = detail.DurationMinutes,
                MaxGroupSize = detail.MaxGroupSize,
                StatusName = StatusName(detail.Status),
                AverageRating = detail.AverageRating,
                ReviewCount = detail.ReviewCount,
                BookingCount = detail.BookingCount,
                IsFeatured = detail.IsFeatured,
                PlaceId = detail.PlaceId,
                CreatedAt = detail.CreatedAt,
                UpdatedAt = detail.UpdatedAt,
                RowVersionBase64 = detail.RowVersion is null ? null : Convert.ToBase64String(detail.RowVersion),
            };
        }

        return vm;
    }

    /// <summary>Maps the TourStatus enum byte value (0-5) to its display name.</summary>
    public static string StatusName(int status) => status switch
    {
        0 => "Draft",
        1 => "Pending",
        2 => "Approved",
        3 => "Rejected",
        4 => "Suspended",
        5 => "Archived",
        _ => "Unknown",
    };

    /// <summary>Maps a tour status name to a UI token color.</summary>
    public static string StatusColor(string statusName) => statusName switch
    {
        "Approved" => "success",
        "Pending" or "Draft" => "warning",
        "Rejected" or "Suspended" => "danger",
        "Archived" => "secondary",
        _ => "secondary",
    };

    /// <summary>
    /// Maps a tour status name to a Font Awesome icon name (used alongside color + text so status is
    /// never conveyed by color alone, per UI-UX-A11Y5).
    /// </summary>
    public static string StatusIcon(string statusName) => statusName switch
    {
        "Approved" => "circle-check",
        "Pending" => "clock",
        "Draft" => "pen-ruler",
        "Rejected" => "circle-xmark",
        "Suspended" => "circle-pause",
        "Archived" => "box-archive",
        _ => "circle-question",
    };
}
