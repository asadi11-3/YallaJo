using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.Logout;


public sealed record LogoutCommand(string RefreshToken) : ICommand;
