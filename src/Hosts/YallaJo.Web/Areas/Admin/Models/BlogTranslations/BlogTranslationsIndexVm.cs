namespace YallaJo.Web.Areas.Admin.Models.BlogTranslations;

/// <summary>Top-level view model for the blog translations index page.</summary>
public sealed class BlogTranslationsIndexVm
{
    public Guid BlogId { get; init; }
    public IReadOnlyList<BlogTranslationRowVm> Translations { get; init; } = [];

    public bool HasArabic { get; init; }
    public bool HasTranslations => Translations.Count > 0;
}

/// <summary>A single translation row in the index list.</summary>
public sealed class BlogTranslationRowVm
{
    public string LanguageCode { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? SummaryPreview { get; init; }
}
