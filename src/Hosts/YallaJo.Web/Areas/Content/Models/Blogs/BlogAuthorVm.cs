namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class BlogAuthorVm
{
    public string DisplayName { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public int ArticleCount { get; init; }
    public int FollowerCount { get; init; }

    public string AuthorImageUrl { get; init; } = string.Empty;
    public string AuthorImageAlt { get; init; } = string.Empty;
    public bool HasAuthorImage { get; init; }
}
