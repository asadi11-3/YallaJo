namespace YallaJo.Web.Areas.Admin.Models.Lookups;

/// <summary>
/// API response item for GET /api/v1/security/users/suggest (F10 typeahead).
/// The Security module has no display-name concept, so the primary email is the label.
/// </summary>
public sealed class UserSuggestResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// API response item for GET /api/v1/places/search/suggest (F10 typeahead).
/// </summary>
public sealed class PlaceSuggestResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}

/// <summary>
/// Normalized lookup item consumed by admin-lookup.js ({ items: [{ id, name }] }).
/// </summary>
public sealed record LookupItemVm(Guid Id, string Name);
