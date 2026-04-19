using AmenityEntity = ContentPlaces.Domain.Entities.BusinessAmenity;

namespace ContentPlaces.Application.Queries.BusinessAmenity.Common;

public sealed record BusinessAmenityDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string? Icon,
    int SortOrder)
{
    public static BusinessAmenityDto From(AmenityEntity amenity) => new(
        amenity.Id,
        amenity.BusinessId,
        amenity.Name,
        amenity.Icon,
        amenity.SortOrder);
}
