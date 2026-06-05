namespace YallaJo.Web.Areas.Business.Models.MyBusinesses;

public static class MyBusinessesMapper
{
    public static MyBusinessesVm ToVm(IReadOnlyList<BusinessSummaryResponse> items)
    {
        return new MyBusinessesVm
        {
            Businesses = items
                .Select(b => new BusinessRowVm(
                    b.Id, b.Name, b.Slug, b.BusinessType, b.Status,
                    b.City, b.Country, b.AverageRating, b.ReviewCount, b.IsVerified))
                .ToList(),
        };
    }

    public static ManageBusinessVm ToManageVm(BusinessDetailResponse d)
    {
        return new ManageBusinessVm
        {
            Id = d.Id,
            Name = d.Name,
            Slug = d.Slug,
            BusinessType = d.BusinessType,
            Status = d.Status,
            RejectionReason = d.RejectionReason,
            IsVerified = d.IsVerified,
            ServiceItemCount = d.ServiceItemCount,
            StaffCount = d.StaffCount,
            AmenityCount = d.AmenityCount,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            Form = new EditBusinessFormVm
            {
                Id = d.Id,
                Name = d.Name,
                PlaceId = d.PlaceId ?? Guid.Empty,
                Latitude = d.Latitude,
                Longitude = d.Longitude,
                Description = d.Description,
                Address = d.Address,
                City = d.City,
                Country = d.Country,
                Phone = d.Phone,
                Email = d.Email,
                Website = d.Website,
            },
        };
    }

    public static string StatusColor(string status) => status?.ToLowerInvariant() switch
    {
        "approved" => "success",
        "pending" => "warning",
        "moredocsneeded" => "warning",
        "rejected" => "danger",
        "suspended" => "danger",
        _ => "secondary",
    };
}
