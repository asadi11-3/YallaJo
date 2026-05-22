using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

/// <summary>
/// Local read-side snapshot of user profile data needed for email rendering.
/// Populated and updated by Auth/Accounts inbox handlers.
/// </summary>
public sealed class UserSnapshot : BaseEntity
{
    private UserSnapshot() { } // EF Core

    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;

    /// <summary>BCP-47 language code (e.g. "en", "ar").</summary>
    public string LanguageCode { get; private set; } = "en";

    // ── Factory / update ──────────────────────────────────────────────────────

    public static UserSnapshot Create(Guid userId, string email, string fullName, string languageCode = "en")
        => new()
        {
            UserId       = userId,
            Email        = email.Trim().ToLowerInvariant(),
            FullName     = fullName.Trim(),
            LanguageCode = languageCode,
        };

    public void Update(string email, string fullName, string languageCode)
    {
        Email        = email.Trim().ToLowerInvariant();
        FullName     = fullName.Trim();
        LanguageCode = languageCode;
    }
}
