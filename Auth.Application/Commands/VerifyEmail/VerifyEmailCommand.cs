using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(
    string Email,
    string OtpCode,
    string RecaptchaToken) : ICommand<VerifyEmailResult> // RECAPTCHA DISABLED: , IRecaptchaProtectedCommand
{
    // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
    // public string RecaptchaAction => RecaptchaActions.VerifyEmail;
}
