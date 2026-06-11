namespace Security.Application.Queries.Dtos;

/// <summary>
/// Minimal identity projection for typeahead pickers and batch enrichment (B1).
/// Deliberately excludes roles, claims and account state (anti-enumeration).
/// </summary>
public sealed record UserLookupDto(
    Guid Id,
    string DisplayName,
    string? Email,
    string? AvatarUrl);
