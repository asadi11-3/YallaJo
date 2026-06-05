namespace YallaJo.Web.Areas.Business.Models.Amenities;

public static class AmenitiesMapper
{
    public static IReadOnlyList<AmenityRowVm> ToRows(IReadOnlyList<BusinessAmenityItemResponse> items) =>
        items
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => new AmenityRowVm(a.Id, a.Name, a.Icon, a.SortOrder))
            .ToList();
}
