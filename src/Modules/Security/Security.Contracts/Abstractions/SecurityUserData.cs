namespace Security.Contracts.Abstractions;

public sealed record SecurityUserData(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> Claims,
    // Phase 2A: defaults to Active so pre-Phase-2A constructions (test
    // doubles, downstream consumers) keep compiling. The Security
    // infrastructure ALWAYS populates this field with the real value;
    // the default exists only to keep the contract source-compatible
    // during the transition.
    AccountLifecycleSnapshot Lifecycle = AccountLifecycleSnapshot.Active);
