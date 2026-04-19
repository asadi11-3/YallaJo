namespace Auth.Application.Interfaces;

/// <summary>
/// Generates and verifies the invite-link token used in the admin onboarding
/// flow. Unlike a 6-digit OTP, the invite token is a URL-safe, high-entropy
/// string embedded in the email link.
/// </summary>
public interface IInviteTokenService
{
    /// <summary>Generates a cryptographically-strong, URL-safe token.</summary>
    string Generate();

    /// <summary>Computes the storage hash for the given plain token.</summary>
    string Hash(string plainToken);

    /// <summary>Verifies the plain token against a stored hash.</summary>
    bool Verify(string plainToken, string hashedToken);
}
