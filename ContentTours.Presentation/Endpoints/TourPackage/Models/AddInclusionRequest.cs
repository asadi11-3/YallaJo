namespace ContentTours.Presentation.Endpoints.TourPackage.Models;

public sealed record AddInclusionRequest(
    string Description,
    int SortOrder
);
