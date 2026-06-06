namespace YallaJo.Web.Areas.Business.Models.MyBusinesses;

/// <summary>Mirrors ContentPlaces UpdateBusinessRequest (owner-editable fields).</summary>
public sealed record UpdateBusinessApiRequest(
    string Name,
    Guid PlaceId,
    decimal Latitude,
    decimal Longitude,
    string? Description,
    string? Address,
    string? City,
    string? Country,
    string? Phone,
    string? Email,
    string? Website,
    bool? IsHalal,
    bool? HasVegetarianOptions,
    bool? HasAlcoholFreeArea);

/// <summary>
/// Mirrors ContentPlaces CreateBusinessRequest. Required: Name, BusinessType,
/// PlaceId, Latitude, Longitude. BusinessType is sent as its enum name string
/// (the API uses a global JsonStringEnumConverter), matching the project
/// convention for request enums.
/// </summary>
public sealed record CreateBusinessApiRequest(
    string Name,
    string BusinessType,
    Guid PlaceId,
    decimal Latitude,
    decimal Longitude,
    string? Slug,
    string? Description,
    string? Address,
    string? City,
    string? Country,
    string? PostalCode,
    string? Phone,
    string? Email,
    string? Website,
    string? LicenseNumber,
    string? TaxId,
    bool? IsHalal,
    bool? HasVegetarianOptions,
    bool? HasAlcoholFreeArea);
