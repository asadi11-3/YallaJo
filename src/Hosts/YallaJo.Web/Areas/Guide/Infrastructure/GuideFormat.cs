using System.Globalization;

namespace YallaJo.Web.Areas.Guide.Infrastructure;

/// <summary>
/// Culture-aware formatting helpers for Guide-area views (CON3). Replaces the
/// per-view Money/Count/Rating/Pct/Date local-function variants that drifted
/// between N2/N3 and yyyy-MM-dd/dd MMM yyyy.
/// </summary>
public static class GuideFormat
{
    private static CultureInfo Culture => CultureInfo.CurrentUICulture;

    /// <summary>Money with 3 decimals (JOD convention, CON3). Falls back to JOD when currency is blank.</summary>
    public static string Money(decimal amount, string? currency = null)
        => amount.ToString("N3", Culture) + " " + (string.IsNullOrWhiteSpace(currency) ? "JOD" : currency);

    public static string Count(int value) => value.ToString("N0", Culture);

    public static string Rating(decimal value) => value.ToString("0.0", Culture);

    /// <summary>Formats a 0–1 ratio as a percentage (e.g. 0.125 → "12.5%").</summary>
    public static string Pct(decimal ratio) => (ratio * 100m).ToString("0.#", Culture) + "%";

    /// <summary>Short ISO-style date (kept stable across cultures; wrap in &lt;bdi dir="ltr"&gt; per RTL3).</summary>
    public static string Date(DateTime value) => value.ToString("yyyy-MM-dd", Culture);

    /// <summary>Date + time for timestamps (wrap in &lt;bdi dir="ltr"&gt; per RTL3).</summary>
    public static string DateTimeShort(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", Culture);
}
