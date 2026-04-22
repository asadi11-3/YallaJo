using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string RecaptchaToken) : ICommand<RegisterResult>, IRecaptchaProtectedCommand
{
    public string RecaptchaAction => RecaptchaActions.Register;
}
