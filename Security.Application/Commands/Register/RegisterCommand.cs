using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Commands.Register;

public sealed record RegisterResult(Guid UserId, string AccessToken);

public sealed record RegisterCommand(string Email, string Password) : ICommand<RegisterResult>;
