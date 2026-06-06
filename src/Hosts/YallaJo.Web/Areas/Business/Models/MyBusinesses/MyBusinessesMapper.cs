using Microsoft.AspNetCore.Mvc.Rendering;

namespace YallaJo.Web.Areas.Business.Models.MyBusinesses;

public static class MyBusinessesMapper
{
    /// <summary>
    /// The ContentPlaces.Domain.Enums.BusinessType values, in display order.
    /// Kept as a static list (rather than reflecting an enum the web project does
    /// not reference) so the picker label and the value posted to the API stay
    /// in lock-step with the backend enum names.
    /// </summary>
    private static readonly (string Value, string Label)[] BusinessTypeChoices =
    {
        ("Restaurant", "Restaurant"),
        ("Hotel", "Hotel"),
        ("Shop", "Shop"),
        ("Agency", "Agency"),
        ("Transport", "Transport"),
        ("Guide", "Guide"),
        ("Activity", "Activity"),
        ("Other", "Other"),
    };

    public static IReadOnlyList<SelectListItem> BusinessTypeOptions(string? selected = null) =>
        BusinessTypeChoices
            .Select(c => new SelectListItem(c.Label, c.Value, string.Equals(c.Value, selected, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    public static IReadOnlyList<SelectListItem> PlaceOptions(IEnumerable<PlaceOptionResponse> places, Guid? selected = null) =>
        places
            .Select(p => new SelectListItem(
                FormatPlaceLabel(p),
                p.Id.ToString("D"),
                selected is { } s && s == p.Id))
            .ToList();

    private static string FormatPlaceLabel(PlaceOptionResponse p)
    {
        var location = string.Join(", ", new[] { p.City, p.Country }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(location) ? p.Name : $"{p.Name} ({location})";
    }

    public static CreateBusinessApiRequest ToCreateRequest(RegisterBusinessFormVm form) => new(
        Name: form.Name.Trim(),
        BusinessType: form.BusinessType.Trim(),
        PlaceId: form.PlaceId,
        Latitude: form.Latitude,
        Longitude: form.Longitude,
        Slug: null,
        Description: NullIfBlank(form.Description),
        Address: NullIfBlank(form.Address),
        City: NullIfBlank(form.City),
        Country: NullIfBlank(form.Country),
        PostalCode: NullIfBlank(form.PostalCode),
        Phone: NullIfBlank(form.Phone),
        Email: NullIfBlank(form.Email),
        Website: NullIfBlank(form.Website),
        LicenseNumber: NullIfBlank(form.LicenseNumber),
        TaxId: NullIfBlank(form.TaxId),
        IsHalal: form.IsHalal,
        HasVegetarianOptions: form.HasVegetarianOptions,
        HasAlcoholFreeArea: form.HasAlcoholFreeArea);

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

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
