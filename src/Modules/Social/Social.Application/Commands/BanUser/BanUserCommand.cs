using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.BanUser;

public sealed record BanUserCommand(
    Guid UserId,
    ReportableEntityType EntityType,
    Guid EntityId,
    string Reason,
    DateTime? ExpiresAt,
    Guid AdminUserId) : ICommand;
