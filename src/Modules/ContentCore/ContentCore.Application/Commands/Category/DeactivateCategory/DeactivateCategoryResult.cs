namespace ContentCore.Application.Commands.Category.DeactivateCategory;

public sealed record DeactivateCategoryResult(
    Guid Id,
    bool IsActive
);