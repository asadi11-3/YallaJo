namespace Auth.Infrastructure.Services;

/// <summary>
/// Configuration for the admin user-invite onboarding flow.
/// </summary>
public sealed class InviteOptions
{
    public const string SectionName = "Invite";

    /// <summary>
    /// URL template for the invite-acceptance page. Supports two placeholders:
    /// <c>{email}</c> and <c>{token}</c>. Example:
    /// <c>https://app.example.com/auth/accept-invite?email={email}&amp;token={token}</c>
    /// </summary>
    public string AcceptUrlTemplate { get; set; } =
        "/auth/accept-invite?email={email}&token={token}";
}
