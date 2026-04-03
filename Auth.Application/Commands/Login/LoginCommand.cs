using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>;
