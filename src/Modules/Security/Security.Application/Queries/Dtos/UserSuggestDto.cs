namespace Security.Application.Queries.Dtos;

/// <summary>
/// Lightweight user projection for admin typeahead lookups.
/// The Security module has no display-name concept, so the primary email doubles as the label.
/// </summary>
public sealed record UserSuggestDto(Guid Id, string Email);
