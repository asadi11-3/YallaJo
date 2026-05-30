using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Category.RestoreCategory;

public sealed record RestoreCategoryCommand(Guid Id) : ICommand<RestoreCategoryResult>;
