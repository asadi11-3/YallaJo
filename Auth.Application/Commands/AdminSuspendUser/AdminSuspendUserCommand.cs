using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AdminSuspendUser;

public sealed record AdminSuspendUserCommand(Guid UserId) : ICommand;
