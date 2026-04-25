namespace Security.Contracts.Abstractions;

/// <summary>
/// Phase 3A — snapshot returned by
/// <see cref="ISecurityService.GetAdminResetEligibilityAsync"/>. Lives in
/// <c>Security.Contracts</c> so Auth does not take a dependency on
/// <c>Security.Domain</c>.
/// </summary>
public sealed record AdminResetEligibility(
    Guid TargetUserId,
    string PrimaryEmail,
    bool IsPrimaryEmailVerified,
    AccountLifecycleSnapshot Lifecycle);
