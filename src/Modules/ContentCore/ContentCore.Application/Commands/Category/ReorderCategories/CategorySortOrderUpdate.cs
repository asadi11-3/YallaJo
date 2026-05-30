namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed record CategorySortOrderUpdate(Guid CategoryId, int SortOrder);
