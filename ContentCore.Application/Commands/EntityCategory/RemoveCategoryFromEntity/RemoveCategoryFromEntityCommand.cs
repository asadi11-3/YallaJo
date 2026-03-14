using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.EntityCategory.RemoveCategoryFromEntity;

public sealed record RemoveCategoryFromEntityCommand(
    string EntityType,
    Guid EntityId,
    Guid CategoryId) : ICommand;
