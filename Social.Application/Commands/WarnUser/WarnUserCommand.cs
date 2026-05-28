using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.WarnUser;

public sealed record WarnUserCommand(
    Guid UserId,
    ReportableEntityType EntityType,
    Guid EntityId,
    string Reason,
    Guid AdminUserId) : ICommand;
