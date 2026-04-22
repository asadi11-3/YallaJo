using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string RecaptchaToken) : ICommand<LoginResult>, IRecaptchaProtectedCommand
{
    public string RecaptchaAction => RecaptchaActions.Login;
}
