using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.EntityTag.AssignTagsToEntity;

public sealed record AssignTagsToEntityCommand(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> TagIds) : ICommand;
