using ContentPlaces.Domain.Enums;
using PlaceEntity = ContentPlaces.Domain.Entities.Place;

namespace ContentPlaces.Application.Queries.Place.Common;

public sealed record PlaceDetailDto(
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
    decimal AverageRating,
    int ReviewCount,
    bool IsFeatured,
    bool IsVerified,
    bool IsWheelchairAccessible,
    bool HasAudioGuide,
    bool HasBrailleSignage,
    string? MetaTitle,
    string? MetaDescription,
    IReadOnlyList<PlaceTranslationDto> Translations)
{
    public static PlaceDetailDto From(PlaceEntity place) => new(
        Id:                    place.Id,
        Name:                  place.Name,
        Slug:                  place.Slug,
        PlaceType:             place.PlaceType,
        Latitude:              place.Location.Latitude,
        Longitude:             place.Location.Longitude,
        Description:           place.Description,
        Address:               place.Address,
        City:                  place.City,
        Country:               place.Country,
        PostalCode:            place.PostalCode,
        Phone:                 place.Phone,
        Email:                 place.Email,
        Website:               place.Website,
        AverageRating:         place.AverageRating,
        ReviewCount:           place.ReviewCount,
        IsFeatured:            place.IsFeatured,
        IsVerified:            place.IsVerified,
        IsWheelchairAccessible: place.IsWheelchairAccessible,
        HasAudioGuide:         place.HasAudioGuide,
        HasBrailleSignage:     place.HasBrailleSignage,
        MetaTitle:             place.MetaTitle,
        MetaDescription:       place.MetaDescription,
        Translations:          place.PlaceTranslations
            .Select(t => new PlaceTranslationDto(t.LanguageId, t.Name, t.Description, t.Address))
            .ToList());
}
