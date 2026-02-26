using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.RefreshToken;

public sealed record RefreshTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResult>;
