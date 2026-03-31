namespace ContentCore.Application.Commands.Category.ReactivateCategory;

public sealed record ReactivateCategoryResult(
    Guid id,
    bool isActive
);
