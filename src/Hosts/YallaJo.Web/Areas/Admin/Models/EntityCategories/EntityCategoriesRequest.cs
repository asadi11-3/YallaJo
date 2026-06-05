namespace YallaJo.Web.Areas.Admin.Models.EntityCategories;

public sealed record AssignCategoriesToEntityRequest(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> CategoryIds);
