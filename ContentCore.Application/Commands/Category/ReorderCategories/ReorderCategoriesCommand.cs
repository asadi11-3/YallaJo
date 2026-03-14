using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed record CategoryOrderItem(Guid CategoryId, int SortOrder);

public sealed record ReorderCategoriesCommand(
    IReadOnlyList<CategoryOrderItem> Items) : ICommand;
