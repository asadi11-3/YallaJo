namespace Auth.Application.Commands.ActivateAccount;

/// <summary>
/// Result payload for <see cref="ActivateAccountCommand"/>. Exposes the
/// activated user id so callers (or the legacy <c>AcceptInvite</c> façade)
/// can surface a post-activation confirmation.
/// </summary>
public sealed record ActivateAccountResult(Guid UserId);
