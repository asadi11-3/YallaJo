namespace Security.Contracts.Abstractions;

/// <summary>
/// Cross-module-safe projection of <c>Security.Domain.Entities.AccountLifecycleState</c>.
/// <para>
/// Defined in <c>Security.Contracts</c> so consumers (Auth, future modules)
/// do not need a project reference on <c>Security.Domain</c>. The ordinals
/// are guaranteed to match the domain enum 1:1 — see Phase 2A commit notes
/// for the mapping discipline (the Security infrastructure layer performs
/// the cast at the contract boundary).
/// </para>
/// </summary>
public enum AccountLifecycleSnapshot
{
    Provisioned = 0,
    PendingActivation = 1,
    Active = 2,
    Suspended = 3,
    PendingPasswordReset = 4,
    Archived = 5,
}
