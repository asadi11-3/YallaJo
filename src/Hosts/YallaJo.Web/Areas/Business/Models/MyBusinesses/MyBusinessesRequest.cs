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
