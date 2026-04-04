using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Presentation.Endpoints.Place.Models;

public sealed record CreatePlaceRequest(
    string Name,
    string? Slug,
    PlaceType PlaceType,
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
