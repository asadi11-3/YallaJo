using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ResendInvite;

public sealed record ResendInviteResult(string Message);

public sealed record ResendInviteCommand(string Email) : ICommand<ResendInviteResult>;
