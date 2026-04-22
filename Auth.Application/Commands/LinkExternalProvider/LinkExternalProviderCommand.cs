using Auth.Application.Recaptcha;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.LinkExternalProvider;

/// <summary>
/// Links an external OAuth provider identity to the current authenticated user.
///
/// <para>
/// The <see cref="Ticket"/> is a short-lived, HMAC-signed, single-use envelope
/// produced by the Web BFF AFTER the end user successfully authenticates against
/// the provider. The API never accepts a raw provider ID from the client — doing
/// so would allow a hostile caller to bind any provider account to their own user.
/// </para>
///
/// <para>
/// <see cref="RecaptchaToken"/> guards against automated link-spam on an
/// already-authenticated session.
/// </para>
/// </summary>
public sealed record LinkExternalProviderCommand(
    string Ticket,
    string RecaptchaToken) : ICommand<Guid>, IRecaptchaProtectedCommand
{
    public string RecaptchaAction => RecaptchaActions.LinkProvider;
}
