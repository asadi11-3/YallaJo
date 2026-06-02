using System.Text.Json;

namespace YallaJo.Web.Areas.Admin.Helpers;

/// <summary>
/// Phase 5A — server-side helper that turns a raw audit
/// <c>Metadata</c> string (compact JSON produced by the backend admin
/// audit writer) into a human-readable representation for the
/// collapsible <c>_MetadataDetails</c> partial.
/// <para>
/// Behavior contract:
/// </para>
/// <list type="bullet">
///   <item><description>Null / empty / whitespace input → <c>(IsEmpty=true, Text="-")</c>.</description></item>
///   <item><description>Valid JSON → <c>(IsValidJson=true, Text=pretty-printed JSON)</c> using <c>WriteIndented = true</c>.</description></item>
///   <item><description>Invalid JSON → <c>(IsValidJson=false, Text=original string)</c> so the view can render it as raw text. Razor's HTML-encoding handles XSS — this helper never builds markup.</description></item>
/// </list>
/// <para>
/// The helper is allocation-light: parsing and re-serializing is done
/// only once per row at render time. No caching is added in Phase 5A —
/// the audit page paginates at 50 rows, well below any sensible budget.
/// </para>
/// </summary>
internal static class AuditMetadataFormatter
{
    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
    };

    public readonly record struct Result(string Text, bool IsEmpty, bool IsValidJson);

    public static Result Format(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return new Result(Text: "-", IsEmpty: true, IsValidJson: false);

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            var pretty = JsonSerializer.Serialize(doc.RootElement, PrettyOptions);
            return new Result(Text: pretty, IsEmpty: false, IsValidJson: true);
        }
        catch (JsonException)
        {
            // Fall back to the original string. Razor will HTML-encode
            // it on render, so XSS-safe by default.
            return new Result(Text: metadata, IsEmpty: false, IsValidJson: false);
        }
    }
}
