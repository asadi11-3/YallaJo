using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.ReactivateCategory;

public sealed record ReactivateCategoryCommand(
    Guid Id
) : ICommand<ReactivateCategoryResult>;
