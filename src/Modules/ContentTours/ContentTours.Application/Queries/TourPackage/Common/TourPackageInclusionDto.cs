namespace ContentTours.Application.Queries.TourPackage.Common;

public sealed record TourPackageInclusionDto(
    Guid Id,
    string Description,
    int SortOrder);
