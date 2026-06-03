namespace ContentBlogs.Application.Queries.BlogTranslation.Dtos;

/// <summary>Admin view of a single blog translation (richer than the public projection).</summary>
public sealed record BlogTranslationAdminDto(
    Guid Id,
    Guid BlogId,
    Guid LanguageId,
    string LanguageCode,
    string Title,
    string Content,
    string? Summary);
