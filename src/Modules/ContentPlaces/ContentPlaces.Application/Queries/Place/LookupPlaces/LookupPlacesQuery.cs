using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.LookupPlaces;

/// <summary>
/// [Backend] B2 — lightweight place typeahead for searchable comboboxes (F10).
/// Name-prefix/contains search; page size clamped to 1-20 (R4).
/// </summary>
public sealed record LookupPlacesQuery(string? Term, int PageSize)
    : IQuery<IReadOnlyList<PlaceLookupDto>>;

/// <summary>Minimal lookup row: id + display name + optional city.</summary>
public sealed record PlaceLookupDto(Guid Id, string Name, string? City);
