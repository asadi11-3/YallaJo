namespace Auth.Application.Invitations;

/// <summary>
/// Constants for the admin-initiated user-invite onboarding flow.
/// The invite token is stored as an Otp row with <see cref="Purpose"/> and
/// <see cref="ExpiryMinutes"/> below (token is URL-safe and considerably
/// longer than an OTP code — its hash is stored in Otp.CodeHash).
/// </summary>
internal static class InviteConstants
{
    public const string Purpose = "UserInvite";

    // 7 days — invite links are expected to be accepted asynchronously, not
    // within minutes like a one-time verification code.
    public const int ExpiryMinutes = 7 * 24 * 60;
}
