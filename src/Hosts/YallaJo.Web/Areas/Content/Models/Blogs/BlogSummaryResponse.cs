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
}
