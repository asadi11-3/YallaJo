namespace ContentCore.Presentation.Endpoints.EntityCategory.Models;

public sealed record AssignCategoriesToEntityRequest(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> CategoryIds);
