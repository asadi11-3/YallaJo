using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AdminReassignAccount;

public sealed record AdminReassignAccountCommand(
    Guid TargetUserId,
    string NewEmail,
    string? Reason) : ICommand<AdminReassignAccountResult>;
