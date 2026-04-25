using Microsoft.AspNetCore.Identity;
using Security.Application.Interfaces;

namespace Security.Infrastructure.Services;

internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
        => _hasher.HashPassword(null!, password);

    /// <summary>
    /// Verifies <paramref name="password"/> against the stored
    /// <paramref name="passwordHash"/>. Returns <c>false</c> for any
    /// stored value that is not a valid Identity v2/v3 hash — never
    /// throws.
    /// <para>
    /// <b>Why the guards:</b> the framework's
    /// <c>PasswordHasher&lt;&gt;.VerifyHashedPassword</c> calls
    /// <c>Convert.FromBase64String</c> on the stored hash before any
    /// format-version check. For users whose <c>PasswordHash</c>
    /// column carries an intentional non-Base64 placeholder
    /// (<c>EXTERNAL-ONLY:&lt;guid&gt;</c> set by
    /// <c>UserRegistrationService.RegisterExternalAsync</c> for
    /// Google/Facebook auto-created users; <c>REASSIGNED:&lt;guid&gt;</c>
    /// set by Phase 3C admin reassignment), that decode throws
    /// <see cref="FormatException"/> and bubbles up as a 500 instead of
    /// the intended 401 "Invalid email or password.". The placeholders
    /// were always meant to be fail-closed; this wrapper now enforces
    /// that contract.
    /// </para>
    /// <para>
    /// Behavior matrix:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Null / empty / whitespace stored hash → <c>false</c>.</description></item>
    ///   <item><description>Stored hash that is not valid Base-64 (placeholder or otherwise) → <c>false</c>.</description></item>
    ///   <item><description>Valid Identity hash + correct password → <c>true</c> (unchanged).</description></item>
    ///   <item><description>Valid Identity hash + wrong password → <c>false</c> (unchanged).</description></item>
    /// </list>
    /// </summary>
    public bool Verify(string password, string passwordHash)
    {
        // Guard 1: null / empty / whitespace → no local credential.
        // The schema marks PasswordHash NOT NULL, so this is defensive,
        // but it costs nothing and prevents an ArgumentNullException
        // surfacing from the framework.
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            return _hasher.VerifyHashedPassword(null!, passwordHash, password)
                is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // Stored hash is not a valid Base-64-encoded ASP.NET
            // Identity hash. This is the documented failure mode of
            // Convert.FromBase64String inside the framework verifier
            // and is exactly the case the EXTERNAL-ONLY: / REASSIGNED:
            // placeholders produce. Fail closed — never throw, never
            // accept.
            return false;
        }
    }
}
