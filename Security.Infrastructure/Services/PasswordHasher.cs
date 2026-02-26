using Microsoft.AspNetCore.Identity;
using Security.Application.Interfaces;

namespace Security.Infrastructure.Services;

internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
        => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash)
        => _hasher.VerifyHashedPassword(null!, passwordHash, password)
           is PasswordVerificationResult.Success
           or PasswordVerificationResult.SuccessRehashNeeded;
}
