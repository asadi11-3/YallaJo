using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AcceptInvite;

public sealed record AcceptInviteResult(Guid UserId);

public sealed record AcceptInviteCommand(
    string Email,
    string Token,
    string Password,
    string ConfirmPassword) : ICommand<AcceptInviteResult>;
