using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Presentation.Endpoints.ServiceItem.Models;

public sealed record CreateServiceItemRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    ServiceCategory Category,
    string? Description = null,
    int SortOrder = 0);
