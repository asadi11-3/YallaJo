namespace YallaJo.Web.Areas.Business.Models.MyBusinesses;

/// <summary>
/// Lightweight place option used to populate the Place picker on the
/// "Register a business" form. Maps the subset of the public places list
/// (GET /api/v1/places) the form needs: an id, a label, and coordinates so the
/// owner's latitude/longitude can be pre-filled from the chosen place.
/// </summary>
public sealed class PlaceOptionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

/// <summary>Wraps the paginated places list returned by GET /api/v1/places.</summary>
public sealed class PlaceOptionsResponse
{
    public List<PlaceOptionResponse> Items { get; set; } = [];
}

/// <summary>
/// Mirrors ContentPlaces PlaceLookupDto (GET /api/v1/places/lookup) — the
/// lightweight typeahead row used by the Register form's place search.
/// </summary>
public sealed class PlaceLookupItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? City { get; set; }
}

/// <summary>Mirrors ContentPlaces BusinessSummaryDto.</summary>
public sealed class BusinessSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string BusinessType { get; set; } = "";
    public string Status { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFeatured { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

/// <summary>Mirrors ContentPlaces BusinessHoursDto.</summary>
public sealed class BusinessHoursResponse
{
    public Guid Id { get; set; }
    public string DayOfWeek { get; set; } = "";
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}

/// <summary>Mirrors ContentPlaces BusinessDetailDto (subset the web app uses).</summary>
public sealed class BusinessDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string BusinessType { get; set; } = "";
    public Guid? PlaceId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFeatured { get; set; }
    public string Status { get; set; } = "";
    public string? RejectionReason { get; set; }
    public int ServiceItemCount { get; set; }
    public int StaffCount { get; set; }
    public int AmenityCount { get; set; }
    public IReadOnlyList<BusinessHoursResponse> BusinessHours { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
