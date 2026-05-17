using Social.Contracts.Services;

namespace Social.Infrastructure.Services;

internal sealed class NoopProfanityFilter : IProfanityFilter
{
    public bool ContainsProfanity(string text) => false;
    public string Sanitize(string text) => text ?? string.Empty;
}
