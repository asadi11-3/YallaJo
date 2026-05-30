using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Agency.AcceptInvitation;

public sealed record AcceptInvitationCommand(Guid InvitationId) : ICommand;
