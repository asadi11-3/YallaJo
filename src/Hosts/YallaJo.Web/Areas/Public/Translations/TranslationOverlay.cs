using System.Globalization;
using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Seo;

namespace YallaJo.Web.Areas.Public.Translations;

public static class TranslationOverlay
{
    public const string DefaultCulture = "ar";

    public static string ActiveLanguageCode
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return string.IsNullOrWhiteSpace(code) ? DefaultCulture : code;
        }
    }

    public static TranslationsResponse Empty { get; } = new();

    public static TranslationsResponse From(ApiResult<TranslationsResponse> result)
        => result is { IsSuccess: true, Data: { } data } ? data : Empty;

    public static async Task<TranslationsResponse> GetApprovedAsync(
        TranslationsApiClient client,
        string entityType,
        Guid entityId,
        string languageCode,
        CancellationToken ct)
    {
        try
        {
            return From(await client.GetAsync(entityType, entityId, languageCode, ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Empty;
        }
    }

    public static string? Apply(
        IReadOnlyList<TranslationEntryResponse>? entries,
        string fieldName,
        string? originalValue,
        string activeCulture,
        string defaultCulture = DefaultCulture)
        => Pick(entries, fieldName, activeCulture)
            ?? Pick(entries, fieldName, defaultCulture)
            ?? originalValue;

    public static SeoContent ApplySeo(
        SeoContent seo,
        IReadOnlyList<TranslationEntryResponse>? entries,
        string activeCulture,
        string? fallbackTitle = null,
        string? fallbackDescription = null,
        string defaultCulture = DefaultCulture)
        => new()
        {
            MetaTitle = Apply(entries, "MetaTitle", seo.MetaTitle ?? fallbackTitle, activeCulture, defaultCulture),
            MetaDescription = Apply(entries, "MetaDescription", seo.MetaDescription ?? fallbackDescription, activeCulture, defaultCulture),
            Canonical = seo.Canonical,
            OgImage = seo.OgImage,
            Faqs = seo.Faqs,
        };

    public static string? Pick(
        IReadOnlyList<TranslationEntryResponse>? entries,
        string fieldName,
        string culture)
    {
        if (entries is null || entries.Count == 0 || string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(culture))
            return null;

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!IsApproved(entry.Status)) continue;
            if (!string.Equals(entry.ToLanguage, culture, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.Equals(entry.FieldName, fieldName, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(entry.TranslatedText)) continue;

            return entry.TranslatedText;
        }

        return null;
    }

    private static bool IsApproved(string? status)
        => string.Equals(status, "HumanReviewed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase);
}
