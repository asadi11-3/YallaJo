namespace YallaJo.Web.Areas.Admin.Models.EntityTags;

/// <summary>
/// Request body for POST /api/v1/content-core/entity-tags (assign tags to an entity).
/// </summary>
public sealed record AssignTagsToEntityRequest(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> TagIds);
