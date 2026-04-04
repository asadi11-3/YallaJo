using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Place.UpdatePlace;

public sealed record UpdatePlaceCommand(
    Guid Id,
    string Name,
    string Slug,
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
    string? MetaDescription) :
    ICommand<UpdatePlaceResult>;

