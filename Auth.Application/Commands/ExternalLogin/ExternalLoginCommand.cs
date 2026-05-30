using Auth.Application.Commands.Login;
using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ExternalLogin;

public sealed record ExternalLoginCommand(
    string Ticket,
    string RecaptchaToken) : ICommand<LoginResult> // RECAPTCHA DISABLED: , IRecaptchaProtectedCommand
{
    // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
    // public string RecaptchaAction => RecaptchaActions.ExternalLogin;
}
