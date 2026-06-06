namespace ContentBlogs.Application.Queries.Blog.Dtos;


public sealed record BlogSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string? Summary,
    DateTime? PublishedAt,
    int ViewCount,
    int? ReadTimeMinutes,
    Guid? PlaceId,
    string LanguageCode,
    bool IsFeatured,
    // ── Creator Backend Contract Polish (Gap 1) — appended, optional ─────────
    // Trailing fields with defaults so existing positional constructions
    // (public ListBlogs, admin queue, creator-blogs) remain compatible.
    // Status: the article's lifecycle (BlogStatus name); empty when not projected.
    // SourceLanguageCode: the language the article was originally written in
    // (resolved from Blog.LanguageId), distinct from the display LanguageCode.
    string Status = "",
    string? SourceLanguageCode = null);
