namespace ContentPlaces.Presentation.Endpoints.BusinessAmenity;

public sealed record AddBusinessAmenityRequest(
    string Name,
    string? Icon,
    int SortOrder);
