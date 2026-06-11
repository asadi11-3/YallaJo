namespace ContentPlaces.Application.Queries.Place.SuggestPlaces;

/// <summary>
/// Lightweight place suggestion for typeahead lookups (id + display name + slug).
/// Mirrors the tours suggest DTO shape so consumers can share rendering logic.
/// </summary>
public sealed record PlaceSuggestDto(Guid Id, string Name, string Slug);
