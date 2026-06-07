namespace YallaJo.Web.Areas.Business.Models.Services;

/// <summary>The lean service-item shape returned by the list endpoint.</summary>
public sealed class ServiceItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public string Currency { get; set; } = "";
}

/// <summary>
/// Service categories mirrored from ContentPlaces.Domain.Enums.ServiceCategory.
/// Sent as a number in JSON bodies (web ApiClient has no JsonStringEnumConverter).
/// </summary>
public enum ServiceCategory : byte
{
    Food = 0,
    Room = 1,
    Ticket = 2,
    Spa = 3,
    Rental = 4,
    Activity = 5,
    Other = 6,
}

public sealed record CreateServiceItemApiRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    ServiceCategory Category,
    string? Description,
    int SortOrder);

/// <summary>Full service detail from GET /places/businesses/services/{id}.
/// Note: the API detail projection does not return Category/Description.</summary>
public sealed class ServiceItemDetailResponse
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public int DurationMinutes { get; init; }
    public int MaxCapacity { get; init; }
    public string Currency { get; init; } = "";
    public int SortOrder { get; init; }
    public bool IsAvailable { get; init; }
}

// PUT /places/businesses/services/{id} — BusinessId is required for IDOR validation.
public sealed record UpdateServiceItemApiRequest(
    Guid BusinessId,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    ServiceCategory Category,
    string? Description,
    int SortOrder);
