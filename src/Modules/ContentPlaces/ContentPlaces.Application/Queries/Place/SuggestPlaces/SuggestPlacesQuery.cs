using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.SuggestPlaces;

/// <summary>
/// Prefix search over place names for typeahead suggestions (top 10 by name).
/// Mirrors <c>SuggestToursQuery</c> in ContentTours.
/// </summary>
public sealed record SuggestPlacesQuery(string Q) : IQuery<IReadOnlyList<PlaceSuggestDto>>;
