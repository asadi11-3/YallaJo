using ContentPlaces.Domain.Enums;

namespace ContentPlaces.Presentation.Endpoints.Business.Models;

public sealed record CreateBusinessRequest(
    string Name,
    string? Slug,
    BusinessType BusinessType,
    Guid PlaceId,
    decimal Latitude,
    decimal Longitude,
    string? Description = null,
    string? Address = null,
    string? City = null,
    string? Country = null,
    string? PostalCode = null,
    string? Phone = null,
    string? Email = null,
    string? Website = null,
    string? LicenseNumber = null,
    string? TaxId = null);
