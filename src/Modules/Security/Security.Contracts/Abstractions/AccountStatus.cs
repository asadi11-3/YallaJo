namespace Security.Contracts.Abstractions;

/// <summary>
/// Lightweight lifecycle snapshot used to gate recovery and login flows.
/// <see cref="IsActive"/> is preserved as a derived convenience for backward
/// compatibility with Phase 1 callers; new code should branch on
/// <see cref="Lifecycle"/> directly.
/// </summary>
public sealed record AccountStatus(
    Guid UserId,
    string Email,
    bool IsActive,
    bool IsEmailVerified,
    // Phase 2A: defaults to a derived guess so pre-Phase-2A test doubles
    // continue to compile. Production code in SecurityService always
    // supplies the real value sourced from User.LifecycleState.
    AccountLifecycleSnapshot Lifecycle = AccountLifecycleSnapshot.Active);
