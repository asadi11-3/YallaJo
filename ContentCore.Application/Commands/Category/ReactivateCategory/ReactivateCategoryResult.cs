namespace ContentCore.Application.Commands.Category.ReactivateCategory;

public sealed record ReactivateCategoryResult(
    Guid Id,
    bool IsActive
);