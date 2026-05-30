using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ActivateAccount;

public sealed record ActivateAccountCommand(
    string Email,
    string Token,
    string Password,
    string ConfirmPassword) : ICommand<ActivateAccountResult>;
