using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ForceRevokeUserSessions;

public sealed record ForceRevokeUserSessionsCommand(Guid UserId) : ICommand;
