namespace YallaJo.Web.Features.Blogs.Models;

// ----- Comment + reaction request bodies -----
public sealed record CreateBlogCommentRequestBody(string Content, Guid? ParentCommentId);

public sealed record BlogReactionRequestBody(string ReactionType);

public sealed class CreateCommentResultResponse
{
    public Guid CommentId { get; init; }
}

// ----- Blog composer request bodies -----
public sealed record CreateBlogRequestBody(
    string Title,
    string Content,
    string SourceLanguageCode,
    string? Slug,
    string? Summary,
    string? MetaTitle,
    string? MetaDescription);

public sealed record UpdateBlogRequestBody(
    string RowVersion,
    string Title,
    string Slug,
    string Content,
    string? Summary,
    string? MetaTitle,
    string? MetaDescription,
    Guid? PlaceId,
    int? ReadTimeMinutes);

public sealed class CreateBlogResultResponse
{
    public Guid BlogId { get; init; }
    public string Slug { get; init; } = string.Empty;
}

public sealed class AdminBlogDetailResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public Guid? PlaceId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;

    // byte[] RowVersion serializes as a base64 string over JSON; round-trip it verbatim.
    public string? RowVersion { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed class MyBlogResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public bool IsFeatured { get; init; }
}
