namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed record ReorderCategoryItemDto(
    Guid Id,
    int SortOrder);