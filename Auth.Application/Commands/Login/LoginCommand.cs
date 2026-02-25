using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.Login;

public sealed record LoginResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>;
