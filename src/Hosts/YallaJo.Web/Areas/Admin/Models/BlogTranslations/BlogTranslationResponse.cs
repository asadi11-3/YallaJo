namespace YallaJo.Web.Areas.Admin.Models.BlogTranslations;

/// <summary>Mirrors <c>BlogTranslationAdminDto</c> from the admin translation endpoints.</summary>
public sealed class BlogTranslationResponse
{
    public Guid Id { get; init; }
    public Guid BlogId { get; init; }
    public Guid LanguageId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Summary { get; init; }
}
