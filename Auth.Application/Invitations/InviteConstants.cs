namespace Auth.Application.Invitations;

/// <summary>
/// Constants for the admin-initiated user-invite onboarding flow.
/// <para>
/// Phase 2C-5 — the <c>Purpose</c> constant that named the legacy
/// <c>Otp</c> row was removed along with the Otp activation path. The
/// activation link is now backed by the
/// <see cref="Auth.Domain.Entities.ActivationToken"/> aggregate; this
/// class is retained as the single source of truth for the invite
/// lifetime so the aggregate factory and any future admin-facing
/// "when does this link expire?" query stay aligned.
/// </para>
/// </summary>
internal static class InviteConstants
{
    // 7 days — invite links are expected to be accepted asynchronously,
    // not within minutes like a one-time verification code.
    public const int ExpiryMinutes = 7 * 24 * 60;
}
