namespace YallaJo.Web.Areas.Admin.Models.BlogTranslations;

/// <summary>
/// Body for <c>PUT /api/v1/blogs/admin/{id}/translations/{languageCode}</c>.
/// BlogTranslation has no Slug/Meta/RowVersion fields, so they are intentionally absent.
/// </summary>
public sealed record UpsertBlogTranslationRequest(
    string Title,
    string Content,
    string? Summary);
