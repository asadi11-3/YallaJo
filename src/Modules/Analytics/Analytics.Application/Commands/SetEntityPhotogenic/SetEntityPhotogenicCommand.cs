using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.SetEntityPhotogenic;

public sealed record SetEntityPhotogenicCommand(EntityType Kind, Guid EntityId, bool IsPhotogenic) : ICommand;
