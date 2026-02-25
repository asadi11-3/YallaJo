namespace Auth.Application.Interfaces;

/// <summary>
/// OTP generation and verification service.
/// Generate produces a cryptographically secure 6-digit code.
/// Hash/Verify use the same algorithm as password hashing for consistency.
/// </summary>
public interface IOtpService
{
    /// <summary>Generates a cryptographically secure 6-digit OTP code.</summary>
    string Generate();

    /// <summary>Hashes a plain OTP code for safe storage.</summary>
    string Hash(string plainOtp);

    /// <summary>Verifies a plain OTP against a stored hash.</summary>
    bool Verify(string plainOtp, string hashedOtp);
}
