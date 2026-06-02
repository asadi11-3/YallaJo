using YallaJo.Web.Areas.Admin.Models.Places;

namespace YallaJo.Web.Areas.Admin.Models.Places;

public sealed record UpdatePlaceRequest(
    string  Name,
    string  Slug,
    PlaceTypeOption PlaceType,
    decimal Latitude,
    decimal Longitude,
    string? Description,
    string? Address,
    string? City,
    string? Country,
    string? PostalCode,
    string? Phone,
    string? Email,
    string? Website,
    string? MetaTitle,
    string? MetaDescription);
