using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.CreateUser;

public sealed record CreateUserCommand(string Email) : ICommand<Guid>;
