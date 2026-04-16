using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.Place.CreatePlace;

public sealed record CreatePlaceCommand(
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
    string? MetaDescription,
    Guid CreatedByUserId) : ICommand<CreatePlaceResult>;
