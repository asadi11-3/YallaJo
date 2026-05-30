using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email,
    string RecaptchaToken) : ICommand<ForgotPasswordResult> // RECAPTCHA DISABLED: , IRecaptchaProtectedCommand
{
    // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
    // public string RecaptchaAction => RecaptchaActions.ForgotPassword;
}
