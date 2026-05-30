using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string RecaptchaToken) : ICommand<LoginResult> // RECAPTCHA DISABLED: , IRecaptchaProtectedCommand
{
    // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
    // public string RecaptchaAction => RecaptchaActions.Login;
}
