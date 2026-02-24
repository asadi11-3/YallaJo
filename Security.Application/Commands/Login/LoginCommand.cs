using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.Login;

public sealed record LoginResult(Guid UserId, string AccessToken);

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>;
