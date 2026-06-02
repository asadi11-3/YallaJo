namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class BlogDetailResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public Guid? PlaceId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public int TourCount { get; init; }
    public List<BlogTourSummaryResponse> LinkedTours { get; init; } = [];
    public bool IsFeatured { get; init; }
}

public sealed class BlogTourSummaryResponse
{
    public Guid TourId { get; init; }
    public int SortOrder { get; init; }
}
