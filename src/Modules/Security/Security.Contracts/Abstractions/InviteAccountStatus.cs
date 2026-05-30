namespace Security.Contracts.Abstractions;

/// <summary>
/// Lightweight onboarding snapshot returned by
/// <see cref="IUserRegistrationService.GetInviteAccountStatusAsync"/>. Used
/// by Auth's invite/activation flows to gate resend/accept/send-activation
/// without exposing the full User aggregate.
/// <para>
/// Phase 2B: <see cref="Lifecycle"/> is the authoritative gate.
/// <see cref="IsActive"/> is retained as a derived convenience for existing
/// callers (it equals <c>Lifecycle == Active</c>); defaults to
/// <see cref="AccountLifecycleSnapshot.Provisioned"/> to keep pre-2B test
/// doubles source-compatible.
/// </para>
/// </summary>
public sealed record InviteAccountStatus(
    Guid UserId,
    string Email,
    bool IsEmailVerified,
    bool IsActive,
    AccountLifecycleSnapshot Lifecycle = AccountLifecycleSnapshot.Provisioned);
