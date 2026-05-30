using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.SendActivationEmail;

public sealed record SendActivationEmailCommand(string Email)
    : ICommand<SendActivationEmailResult>;
