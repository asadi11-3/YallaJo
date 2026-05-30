using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>Single blocked word entry in the profanity filter blocklist.</summary>
public sealed class ProfanityBlocklistEntry : BaseEntity
{
    private ProfanityBlocklistEntry() { } // EF Core

    public ProfanityBlocklistEntry(string word, string languageCode)
    {
        Word = word.ToLowerInvariant().Trim();
        LanguageCode = languageCode.ToLowerInvariant();
    }

    /// <summary>The blocked word (stored lower-case).</summary>
    public string Word { get; private set; } = string.Empty;

    /// <summary>ISO 639-1 language code, e.g. "en", "ar".</summary>
    public string LanguageCode { get; private set; } = "en";
}
