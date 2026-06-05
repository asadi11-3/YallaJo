namespace YallaJo.Web.Areas.Admin.Models.EntityTags;

/// <summary>
/// A tag currently assigned to an entity. Mirrors backend EntityTagDto.
/// </summary>
public sealed record EntityTagItemResponse(Guid TagId, string Name, string Slug);
