namespace ContentPlaces.Application.Commands.Place.UpdatePlace;

public sealed record UpdatePlaceResult(Guid PlaceId, string Name, string Slug);
