using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.ActivateUser;

public sealed record ActivateUserCommand(Guid UserId) : ICommand;
