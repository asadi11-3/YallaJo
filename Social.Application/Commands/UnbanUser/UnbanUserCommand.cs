using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.UnbanUser;

public sealed record UnbanUserCommand(Guid UserId, Guid AdminUserId) : ICommand;
