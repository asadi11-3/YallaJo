namespace ContentCore.Presentation.Endpoints.EntityTag.Models;

public sealed record AssignTagsToEntityRequest(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> TagIds);
