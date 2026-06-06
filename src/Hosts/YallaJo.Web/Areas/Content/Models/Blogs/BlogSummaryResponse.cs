namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class BlogSummaryResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public Guid? PlaceId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }

    // ── Creator Backend Contract Polish (Gap 1) ─────────────────────────────
    // The article's lifecycle status (BlogStatus name) for the My Articles list.
    public string Status { get; init; } = string.Empty;
    // The language the article was originally written in (resolved server-side).
    public string? SourceLanguageCode { get; init; }
}
