namespace Security.Contracts.Abstractions;

/// <summary>
/// Phase 3C — result payload for
/// <see cref="ISecurityService.ReassignUserByAdminAsync"/>. Carries the
/// addresses and lifecycle snapshot the Auth-side handler needs to
/// supersede tokens, deactivate external providers, revoke sessions,
/// and issue a fresh activation email to the new address.
/// </summary>
public sealed record ReassignmentCompleted(
    Guid TargetUserId,
    string OldEmail,
    string NewEmail,
    AccountLifecycleSnapshot Lifecycle);
