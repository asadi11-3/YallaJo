using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Business.UpdateBusiness;

public sealed record UpdateBusinessCommand(
    Guid Id,
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
    string? Website = null) : ICommand;
