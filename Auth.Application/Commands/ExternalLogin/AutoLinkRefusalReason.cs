namespace Auth.Application.Commands.ExternalLogin;

/// <summary>
/// Every possible reason the secure external-login resolver can fail closed.
///
/// <para>
/// The enum name is written verbatim into the Warning log line each time
/// sign-in is refused. The client always gets the same generic 401 message —
/// this enum is the ONLY operator-visible signal for which safety gate
/// tripped.
/// </para>
/// </summary>
internal enum AutoLinkRefusalReason
{
    None = 0,
    TicketMissingEmail,
    ProviderDidNotVerifyEmail,
    LocalAccountInactive,
    LocalEmailNotVerified,
    ResolvedEmailMismatch,
    UserAlreadyHasDifferentLinkOnSameProvider,
    ProviderIdentityOwnedByAnotherUser,
    ConcurrentAccountCreated,
    AutoCreateFailed,
}
