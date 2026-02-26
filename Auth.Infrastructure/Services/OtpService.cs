using System.Security.Cryptography;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Generates cryptographically secure 6-digit OTPs and hashes/verifies them
/// using ASP.NET Identity's PasswordHasher for consistency with password hashing.
/// </summary>
internal sealed class OtpService : IOtpService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Generate()
    {
        // Generate a cryptographically secure random number in [0, 999999]
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return code.ToString("D6"); // zero-padded to 6 digits
    }

    public string Hash(string plainOtp)
        => _hasher.HashPassword(null!, plainOtp);

    public bool Verify(string plainOtp, string hashedOtp)
        => _hasher.VerifyHashedPassword(null!, hashedOtp, plainOtp)
           is PasswordVerificationResult.Success
           or PasswordVerificationResult.SuccessRehashNeeded;
}
