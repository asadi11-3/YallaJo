using Social.Contracts.Services;

namespace Social.Infrastructure.Services;

/// <summary>No-op profanity filter — never flags content. Left as fallback stub.</summary>
internal sealed class NoopProfanityFilter : IProfanityFilter
{
    public bool ContainsProfanity(string text, string? languageCode = null) => false;
    public string Sanitize(string text, string? languageCode = null) => text ?? string.Empty;
}
