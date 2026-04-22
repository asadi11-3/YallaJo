using Auth.Application.Commands.Login;
using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ExternalLogin;

/// <summary>
/// Signs the user in using a previously-linked external provider identity.
///
/// <para>
/// The <see cref="Ticket"/> is a short-lived, HMAC-signed, single-use envelope
/// produced by the Web BFF after the end user successfully authenticates against
/// the external provider. If no active <c>ExternalProvider</c> row maps the
/// ticket's (provider, providerUserId) to a user, login is refused — the flow
/// deliberately does not auto-provision new accounts from external identities
/// because doing so would blindly trust a provider-asserted email address.
/// </para>
///
/// <para>
/// <see cref="RecaptchaToken"/> guards against automated abuse of the external
/// login endpoint (e.g. replaying scraped tickets, credential-stuffing bots).
/// </para>
/// </summary>
public sealed record ExternalLoginCommand(
    string Ticket,
    string RecaptchaToken) : ICommand<LoginResult>, IRecaptchaProtectedCommand
{
    public string RecaptchaAction => RecaptchaActions.ExternalLogin;
}
