using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.CreateBusiness;

public sealed record CreateBusinessCommand(
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
    string? TaxId = null,
    bool? IsHalal = null,
    bool? HasVegetarianOptions = null,
    bool? HasAlcoholFreeArea = null) : ICommand<CreateBusinessResult>;
