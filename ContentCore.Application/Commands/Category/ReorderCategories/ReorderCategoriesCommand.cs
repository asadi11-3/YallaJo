using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.ReorderCategories;

public sealed record CategoryOrderItem(Guid CategoryId, int SortOrder);

/// <summary>Batch-update SortOrder for a set of categories in a single DB roundtrip.</summary>
public sealed record ReorderCategoriesCommand(
    IReadOnlyList<CategoryOrderItem> Items) : ICommand;
