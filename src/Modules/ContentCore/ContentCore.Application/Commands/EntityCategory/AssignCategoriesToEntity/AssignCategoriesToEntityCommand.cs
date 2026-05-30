using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;

public sealed record AssignCategoriesToEntityCommand(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> CategoryIds) : ICommand;
