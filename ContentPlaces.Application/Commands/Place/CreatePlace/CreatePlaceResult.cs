namespace ContentPlaces.Application.Commands.Place.CreatePlace;

public sealed record CreatePlaceResult(Guid PlaceId, string Name, string Slug);
