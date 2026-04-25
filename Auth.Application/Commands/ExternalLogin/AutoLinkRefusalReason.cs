namespace Auth.Application.Commands.ExternalLogin;

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
