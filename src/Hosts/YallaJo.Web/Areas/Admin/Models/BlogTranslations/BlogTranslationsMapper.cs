namespace YallaJo.Web.Areas.Admin.Models.BlogTranslations;

/// <summary>DTO ↔ ViewModel ↔ Request mapping for the admin blog translation screens.</summary>
public static class BlogTranslationsMapper
{
    private const int SummaryPreviewLength = 120;

    public static BlogTranslationsIndexVm ToIndexVm(
        Guid blogId, IReadOnlyList<BlogTranslationResponse> translations) => new()
    {
        BlogId       = blogId,
        Translations = translations.Select(ToRowVm).ToList(),
        HasArabic    = translations.Any(t =>
            string.Equals(t.LanguageCode, "ar", StringComparison.OrdinalIgnoreCase)),
    };

    public static BlogTranslationRowVm ToRowVm(BlogTranslationResponse r) => new()
    {
        LanguageCode   = r.LanguageCode,
        Title          = r.Title,
        SummaryPreview = Preview(r.Summary),
    };

    /// <summary>Builds an edit VM from an existing translation.</summary>
    public static BlogTranslationEditVm ToEditVm(Guid blogId, BlogTranslationResponse r) => new()
    {
        BlogId       = blogId,
        LanguageCode = r.LanguageCode,
        IsNew        = false,
        Title        = r.Title,
        Content      = r.Content,
        Summary      = r.Summary,
    };

    /// <summary>Builds an empty edit VM for a language that has no translation yet.</summary>
    public static BlogTranslationEditVm ToNewVm(Guid blogId, string languageCode) => new()
    {
        BlogId       = blogId,
        LanguageCode = languageCode,
        IsNew        = true,
    };

    public static UpsertBlogTranslationRequest ToRequest(BlogTranslationEditVm vm) => new(
        Title:   vm.Title.Trim(),
        Content: vm.Content.Trim(),
        Summary: string.IsNullOrWhiteSpace(vm.Summary) ? null : vm.Summary.Trim());

    private static string? Preview(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return null;
        var s = summary.Trim();
        return s.Length <= SummaryPreviewLength ? s : s[..SummaryPreviewLength] + "…";
    }
}
