using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Social.Contracts.Services;
using Social.Infrastructure.Persistence;

namespace Social.Infrastructure.Services;

/// <summary>
/// Sub-millisecond profanity filter backed by the <c>social.ProfanityBlocklistEntries</c> table.
/// Loads the entire blocklist into an in-memory HashSet on first use (lazy, thread-safe).
/// Registered as Singleton; the word list is refreshed when the host is restarted.
/// </summary>
internal sealed class BlocklistProfanityFilter(
    IServiceScopeFactory scopeFactory,
    ILogger<BlocklistProfanityFilter> logger) : IProfanityFilter
{
    // languageCode (lowercased) → set of words
    private Dictionary<string, HashSet<string>>? _blocklist;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool ContainsProfanity(string text, string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var list = GetBlocklist();
        return CheckText(text, languageCode, list);
    }

    public string Sanitize(string text, string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
        var list = GetBlocklist();
        var words = list.TryGetValue(languageCode?.ToLowerInvariant() ?? "en", out var langSet)
            ? langSet
            : list.SelectMany(kv => kv.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = text;
        foreach (var word in words)
        {
            result = result.Replace(word, new string('*', word.Length), StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static bool CheckText(string text, string? languageCode,
        Dictionary<string, HashSet<string>> list)
    {
        var lang = languageCode?.ToLowerInvariant() ?? "en";
        // Check language-specific list; fall back to "en"
        var set = list.TryGetValue(lang, out var langSet) ? langSet
            : list.TryGetValue("en", out var enSet) ? enSet
            : null;

        if (set is null || set.Count == 0) return false;

        // Normalize text: split by whitespace and punctuation
        var tokens = text.Split(' ', '\t', '\n', '\r', '.', ',', '!', '?');
        return tokens.Any(t => set.Contains(t.ToLowerInvariant()));
    }

    /// <summary>Returns cached blocklist, loading from DB on first call.</summary>
    private Dictionary<string, HashSet<string>> GetBlocklist()
    {
        if (_blocklist is not null) return _blocklist;

        _lock.Wait();
        try
        {
            if (_blocklist is not null) return _blocklist;
            _blocklist = LoadFromDb();
        }
        finally
        {
            _lock.Release();
        }

        return _blocklist;
    }

    private Dictionary<string, HashSet<string>> LoadFromDb()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<SocialDbContext>();
            var entries = ctx.ProfanityBlocklistEntries
                .AsNoTracking()
                .Select(e => new { e.Word, e.LanguageCode })
                .ToList();

            var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                var lang = entry.LanguageCode.ToLowerInvariant();
                if (!dict.TryGetValue(lang, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    dict[lang] = set;
                }
                set.Add(entry.Word.ToLowerInvariant());
            }

            logger.LogInformation("BlocklistProfanityFilter loaded {Count} words from DB.", entries.Count);
            return dict;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BlocklistProfanityFilter failed to load from DB — using empty list.");
            return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
