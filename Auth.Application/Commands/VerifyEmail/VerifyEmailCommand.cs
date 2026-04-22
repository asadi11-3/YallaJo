using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(
    string Email,
    string OtpCode,
    string RecaptchaToken) : ICommand<VerifyEmailResult>, IRecaptchaProtectedCommand
{
    public string RecaptchaAction => RecaptchaActions.VerifyEmail;
}
