namespace YallaJo.Web.Areas.Admin.Models.Tours;

public static class AdminToursMapper
{
    public static AdminToursIndexVm ToIndexVm(
        AdminToursPaginatedResponse<AdminTourSummaryResponse> page, string? status) => new()
    {
        Status          = status,
        Items           = page.Items.Select(ToRowVm).ToList(),
        PageNumber      = page.PageNumber,
        PageSize        = page.PageSize,
        TotalCount      = page.TotalCount,
        TotalPages      = page.TotalPages,
        HasPreviousPage = page.HasPreviousPage,
        HasNextPage     = page.HasNextPage,
    };

    public static AdminTourRowVm ToRowVm(AdminTourSummaryResponse r) => new()
    {
        Id               = r.Id,
        Name             = r.Name,
        Slug             = r.Slug,
        Status           = r.Status,
        StatusBadgeClass = BadgeClass(r.Status),
        BasePrice        = r.BasePrice,
        Currency         = r.Currency,
        CreatedAt        = r.CreatedAt,
        SubmittedAt      = r.SubmittedAt,
        CreatedByUserId  = r.CreatedByUserId,
    };

    public static AdminTourDetailsVm ToDetailsVm(AdminTourDetailResponse r) => new()
    {
        Id                    = r.Id,
        Name                  = r.Name,
        Slug                  = r.Slug,
        Description           = r.Description,
        ShortDescription      = r.ShortDescription,
        Difficulty            = r.Difficulty,
        DurationMinutes       = r.DurationMinutes,
        MaxGroupSize          = r.MaxGroupSize,
        MinAge                = r.MinAge,
        BasePrice             = r.BasePrice,
        Currency              = r.Currency,
        Latitude              = r.Latitude,
        Longitude             = r.Longitude,
        MeetingPointLatitude  = r.MeetingPointLatitude,
        MeetingPointLongitude = r.MeetingPointLongitude,
        Status                = r.Status,
        StatusBadgeClass      = BadgeClass(r.Status),
        IsInstantBooking      = r.IsInstantBooking,
        CancellationPolicyHours = r.CancellationPolicyHours,
        IsChildFriendly       = r.IsChildFriendly,
        IsAccessible          = r.IsAccessible,
        AgeRestriction        = r.AgeRestriction,
        PlaceId               = r.PlaceId,
        CreatedAt             = r.CreatedAt,
        UpdatedAt             = r.UpdatedAt,
        HasRowVersion         = r.RowVersion is { Length: > 0 },
    };

    private static string BadgeClass(string status) => status switch
    {
        "Approved"  => "bg-success",
        "Pending"   => "bg-warning text-dark",
        "Rejected"  => "bg-danger",
        "Suspended" => "bg-secondary",
        "Archived"  => "bg-dark",
        "Draft"     => "bg-light text-dark border",
        _           => "bg-secondary",
    };
}
