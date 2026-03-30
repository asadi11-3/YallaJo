using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.ReorderCategories;



public sealed record ReorderCategoriesCommand(
    IReadOnlyList<CategorySortOrderUpdate> SortOrders) : ICommand;
