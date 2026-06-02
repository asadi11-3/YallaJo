namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class BlogDetailsVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Sanitized blog body HTML, safe to render with <c>Html.Raw</c>. Produced by
    /// <see cref="Helpers.ContentHtmlSanitizer"/> in the mapper — the raw API content
    /// never reaches the view.
    /// </summary>
    public string ContentHtml { get; init; } = string.Empty;

    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public bool IsFeatured { get; init; }
    public int TourCount { get; init; }

    public string CoverImageUrl { get; init; } = string.Empty;
    public string CoverImageAlt { get; init; } = string.Empty;
    public bool HasCoverImage { get; init; }

    public BlogAuthorVm? Author { get; init; }
    public IReadOnlyList<BlogCommentVm> Comments { get; init; } = [];
    public IReadOnlyList<TagVm> PopularTags { get; init; } = [];

    public bool HasAuthor => Author is not null;
    public bool HasComments => Comments.Count > 0;
    public int CommentCount => Comments.Count;
}
