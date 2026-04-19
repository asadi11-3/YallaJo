namespace ContentPlaces.Presentation.Endpoints.ServiceItem.Models;

public sealed record UpdateServiceItemRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    int SortOrder);
