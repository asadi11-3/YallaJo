using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;

public sealed record RemoveTagFromEntityCommand(
    string EntityType,
    Guid EntityId,
    Guid TagId) : ICommand;
