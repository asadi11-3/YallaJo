using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.RedactAuditLog;

public sealed record RedactAuditLogCommand(long Id, Guid AdminUserId, string Reason) : ICommand;
