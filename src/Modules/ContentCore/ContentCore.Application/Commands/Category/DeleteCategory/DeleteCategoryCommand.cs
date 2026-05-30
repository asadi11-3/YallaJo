using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand;
