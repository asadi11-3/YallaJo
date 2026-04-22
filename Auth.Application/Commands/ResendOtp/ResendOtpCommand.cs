using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ResendOtp;

public sealed record ResendOtpCommand(
    string Email,
    string Purpose,
    string RecaptchaToken) : ICommand<ResendOtpResult>, IRecaptchaProtectedCommand
{
    public string RecaptchaAction => RecaptchaActions.ResendOtp;
}
