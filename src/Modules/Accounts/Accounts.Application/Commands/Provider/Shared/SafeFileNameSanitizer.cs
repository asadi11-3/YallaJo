namespace Accounts.Application.Commands.Provider.Shared;

/// <summary>
/// Sanitizes original file names for safe use in Content-Disposition headers and
/// stored <c>SafeFileName</c> columns. Strips any path components a malicious
/// original might carry and replaces filesystem-invalid characters with '_'.
/// Shared between the Patch 1A download endpoint and the Patch 2B backfill so
/// both produce identical SafeFileName values for the same input.
/// </summary>
internal static class SafeFileNameSanitizer
{
    /// <summary>
    /// Returns a filesystem-safe file name. If the input is blank or sanitizes to
    /// blank, returns the supplied <paramref name="fallback"/>.
    /// </summary>
    public static string Sanitize(string? originalFileName, string fallback)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            return fallback;

        // Drop any path components a malicious original name might carry.
        var name = Path.GetFileName(originalFileName.Trim());

        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }
}
