using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ContentBlogs.Application.Commands.Creator.Common;

/// <summary>
/// Generates URL-friendly slugs for Creator profiles from display names.
/// On collision, appends a random 4-character alphanumeric suffix (up to 3 retries).
/// </summary>
internal static class CreatorSlugGenerator
{
    /// <summary>
    /// Maximum slug length for creator profile slugs.
    /// </summary>
    public const int MaxSlugLength = 100;

    /// <summary>
    /// Number of collision-resolution retries before giving up.
    /// </summary>
    public const int MaxCollisionRetries = 3;

    private static readonly Regex InvalidCharsRegex =
        new("[^a-z0-9-]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly Regex MultipleDashesRegex =
        new("-{2,}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private const string AlphanumericChars = "abcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// Generates a unique slug from <paramref name="displayName"/>, avoiding any slug in
    /// <paramref name="existingSlugs"/>.  Returns <c>null</c> only if all retries are exhausted.
    /// </summary>
    /// <param name="displayName">The creator's display name to slugify.</param>
    /// <param name="existingSlugs">Set of slugs already taken (case-insensitive comparison assumed).</param>
    /// <returns>A unique slug, or <c>null</c> if collision resolution failed after max retries.</returns>
    public static string? Generate(string displayName, IReadOnlySet<string> existingSlugs)
    {
        var baseSlug = FromDisplayName(displayName);

        if (string.IsNullOrEmpty(baseSlug))
            return null;

        if (!existingSlugs.Contains(baseSlug))
            return baseSlug;

        // Collision detected — append random suffix up to MaxCollisionRetries times
        for (var i = 0; i < MaxCollisionRetries; i++)
        {
            var suffix = GenerateRandomSuffix(4);
            var candidate = TruncateWithSuffix(baseSlug, suffix);

            if (!existingSlugs.Contains(candidate))
                return candidate;
        }

        return null;
    }

    private static string FromDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return string.Empty;

        // Decompose accented characters and strip non-ASCII marks (e.g. "café" → "cafe")
        var normalized = displayName.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        var ascii = builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Trim()
            .Replace(' ', '-')
            .Replace('_', '-');

        ascii = InvalidCharsRegex.Replace(ascii, string.Empty);
        ascii = MultipleDashesRegex.Replace(ascii, "-").Trim('-');

        if (ascii.Length > MaxSlugLength)
        {
            ascii = ascii[..MaxSlugLength].TrimEnd('-');
            ascii = MultipleDashesRegex.Replace(ascii, "-").Trim('-');
        }

        return ascii;
    }

    private static string TruncateWithSuffix(string baseSlug, string suffix)
    {
        var maxBaseLength = MaxSlugLength - suffix.Length - 1; // -1 for the dash separator
        var truncated = baseSlug.Length > maxBaseLength
            ? baseSlug[..maxBaseLength].TrimEnd('-')
            : baseSlug;

        return $"{truncated}-{suffix}";
    }

    private static string GenerateRandomSuffix(int length)
    {
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = AlphanumericChars[Random.Shared.Next(AlphanumericChars.Length)];
        }

        return new string(buffer);
    }
}
