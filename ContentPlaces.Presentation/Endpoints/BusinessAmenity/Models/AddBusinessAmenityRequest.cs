namespace ContentPlaces.Presentation.Endpoints.BusinessAmenity.Models;

public sealed record AddBusinessAmenityRequest(
    string Name,
    string? Icon,
    int SortOrder);
