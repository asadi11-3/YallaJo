using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed record ReorderCategoriesCommand(
    List<ReorderCategoryItemDto> Items
) : ICommand<ReorderCategoriesResult>;