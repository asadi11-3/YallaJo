namespace ContentPlaces.Presentation.Endpoints.Business.Models;

public sealed record UpdateBusinessRequest(
    string Name,
    Guid? PlaceId,
    decimal Latitude,
    decimal Longitude,
    string? Description = null,
    string? Address = null,
    string? City = null,
    string? Country = null,
    string? Phone = null,
    string? Email = null,
    string? Website = null);
