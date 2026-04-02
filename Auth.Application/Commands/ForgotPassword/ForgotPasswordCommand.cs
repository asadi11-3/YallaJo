using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand<ForgotPasswordResult>;
