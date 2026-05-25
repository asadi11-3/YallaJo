using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Agency.DeclineInvitation;

public sealed record DeclineInvitationCommand(Guid InvitationId) : ICommand;
