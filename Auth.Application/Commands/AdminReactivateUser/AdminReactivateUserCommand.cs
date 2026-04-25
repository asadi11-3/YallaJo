using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AdminReactivateUser;

public sealed record AdminReactivateUserCommand(Guid UserId) : ICommand;
