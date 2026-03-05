using System.Security.Cryptography;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Services;


internal sealed class OtpService : IOtpService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Generate()
    {
       
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return code.ToString("D6"); 
    }

    public string Hash(string plainOtp)
        => _hasher.HashPassword(null!, plainOtp);

    public bool Verify(string plainOtp, string hashedOtp)
        => _hasher.VerifyHashedPassword(null!, hashedOtp, plainOtp)
           is PasswordVerificationResult.Success
           or PasswordVerificationResult.SuccessRehashNeeded;
}
