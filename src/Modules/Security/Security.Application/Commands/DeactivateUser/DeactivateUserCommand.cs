using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
