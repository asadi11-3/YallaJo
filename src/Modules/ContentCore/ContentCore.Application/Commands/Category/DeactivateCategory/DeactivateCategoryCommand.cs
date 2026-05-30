using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.DeactivateCategory;

public sealed record DeactivateCategoryCommand(
    Guid Id
) : ICommand<DeactivateCategoryResult>;
