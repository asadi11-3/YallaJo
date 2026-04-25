using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email,
    string RecaptchaToken) : ICommand<ForgotPasswordResult>, IRecaptchaProtectedCommand
{
    public string RecaptchaAction => RecaptchaActions.ForgotPassword;
}
