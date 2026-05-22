namespace Social.Contracts.Services;

/// <summary>
/// Sub-millisecond profanity detection. Used inline during review creation (S-R4).
/// Implementations must be synchronous and thread-safe (registered as Singleton).
/// </summary>
public interface IProfanityFilter
{
    /// <summary>Returns true if the text contains profanity.</summary>
    bool ContainsProfanity(string text, string? languageCode = null);

    /// <summary>Returns a sanitized version of the text with profanity replaced by asterisks.</summary>
    string Sanitize(string text, string? languageCode = null);
}
