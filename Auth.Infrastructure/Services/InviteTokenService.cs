using System.Security.Cryptography;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Produces a 32-byte URL-safe token (Base64Url) and hashes it using the same
/// ASP.NET Core <see cref="PasswordHasher{TUser}"/> primitive used for OTPs.
/// Only the hash is persisted — the plain token is only ever in the invite
/// email link.
/// </summary>
internal sealed class InviteTokenService : IInviteTokenService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Generate()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    public string Hash(string plainToken) =>
        _hasher.HashPassword(null!, plainToken);

    public bool Verify(string plainToken, string hashedToken) =>
        _hasher.VerifyHashedPassword(null!, hashedToken, plainToken)
            is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
