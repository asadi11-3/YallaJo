using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.LinkExternalProvider;


public sealed record LinkExternalProviderCommand(
    string Ticket,
    string RecaptchaToken) : ICommand<Guid> // RECAPTCHA DISABLED: , IRecaptchaProtectedCommand
{
    // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
    // public string RecaptchaAction => RecaptchaActions.LinkProvider;
}
