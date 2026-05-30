using System.Globalization;

namespace YallaJo.Api.Middleware;

/// <summary>
/// Parses the <c>Accept-Language</c> header and sets <see cref="CultureInfo"/>
/// for the current request. Defaults to <c>en</c> if no supported language is found.
/// </summary>
public sealed class RequestLocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "en"
    };
    private const string DefaultLanguage = "en";

    public RequestLocalizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var acceptLanguage = context.Request.Headers.AcceptLanguage.ToString();
        var languageCode = ParsePreferredLanguage(acceptLanguage);

        var culture = new CultureInfo(languageCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        // Make the resolved language code available downstream
        context.Items["Language"] = languageCode;

        await _next(context);
    }

    private static string ParsePreferredLanguage(string? acceptLanguageHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
            return DefaultLanguage;

        // Parse quality-weighted Accept-Language values (e.g. "ar;q=0.9, en;q=0.8")
        var languages = acceptLanguageHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseLanguageEntry)
            .OrderByDescending(x => x.Quality)
            .Select(x => x.Code);

        foreach (var lang in languages)
        {
            // Check exact match first, then base language (e.g. "ar-SA" → "ar")
            if (SupportedLanguages.Contains(lang))
                return lang.ToLowerInvariant();

            var baseLang = lang.Split('-')[0];
            if (SupportedLanguages.Contains(baseLang))
                return baseLang.ToLowerInvariant();
        }

        return DefaultLanguage;
    }

    private static (string Code, double Quality) ParseLanguageEntry(string entry)
    {
        var parts = entry.Split(';', StringSplitOptions.TrimEntries);
        var code = parts[0];
        var quality = 1.0;

        if (parts.Length > 1 && parts[1].StartsWith("q=", StringComparison.OrdinalIgnoreCase))
        {
            double.TryParse(parts[1][2..], NumberStyles.Float, CultureInfo.InvariantCulture, out quality);
        }

        return (code, quality);
    }
}
