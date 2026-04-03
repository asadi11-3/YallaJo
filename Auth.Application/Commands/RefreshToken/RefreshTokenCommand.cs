using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResult>;
