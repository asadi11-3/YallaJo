using System.Globalization;

namespace YallaJo.Web.Areas.Provider.Models.Shared;

/// <summary>
/// Centralized display formatting for the Provider area (UI-UX CON3: currency is
/// JOD with 3 decimals; dates/counts honor the current UI culture). Imported once
/// via <c>@using static</c> in the area <c>_ViewImports.cshtml</c> so views call
/// <c>Money(...)</c> / <c>Count(...)</c> / <c>Date(...)</c> directly — replaces
/// ~14 copy-pasted local helper blocks with inconsistent N0/N2/N3 formats.
/// Views with intentionally different semantics (ISO dates on Provider/Status,
/// booking-currency fallback on ProviderBookings/Details) keep local overrides,
/// which shadow these imports.
/// </summary>
public static class ProviderFormat
{
    /// <summary>Formats an amount per CON3: 3 decimals + currency code (JOD fallback).</summary>
    public static string Money(decimal amount, string? currency = null) =>
        amount.ToString("N3", CultureInfo.CurrentUICulture)
        + " "
        + (string.IsNullOrWhiteSpace(currency) ? "JOD" : currency);

    /// <summary>Formats an integer count with culture-aware group separators.</summary>
    public static string Count(int n) => n.ToString("N0", CultureInfo.CurrentUICulture);

    /// <summary>Formats a date as "d MMM yyyy" with culture-localized month names.</summary>
    public static string Date(DateTime dt) => dt.ToString("d MMM yyyy", CultureInfo.CurrentUICulture);

    /// <summary>Formats a date-only value as "d MMM yyyy" with culture-localized month names.</summary>
    public static string Date(DateOnly d) => d.ToString("d MMM yyyy", CultureInfo.CurrentUICulture);
}
