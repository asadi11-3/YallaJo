using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Public.Models.Blog;

public sealed record CreateBlogCommentBody(string Content, Guid? ParentCommentId = null);

public sealed record EditBlogCommentBody(byte[] RowVersion, string Content);

public sealed record DeleteBlogCommentBody(byte[] RowVersion);

public sealed record AddBlogCommentReactionBody(int ReactionType = 0);

public sealed class BlogCommentResponse
{
    public Guid Id { get; init; }
    public Guid BlogId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public Guid UserId { get; init; }
    public string Content { get; init; } = "";
    public bool IsContentRedacted { get; init; }
    public int LikeCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public sealed class BlogCommentPageResponse
{
    public List<BlogCommentResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

public sealed class BlogCommentVm
{
    public Guid Id { get; init; }
    public Guid BlogId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public Guid UserId { get; init; }
    public string Content { get; init; } = "";
    public bool IsContentRedacted { get; init; }
    public int LikeCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
    public IReadOnlyList<BlogCommentVm> Replies { get; set; } = [];
    public bool HasReplies => Replies.Count > 0;
    public bool IsWithinEditWindow => !IsContentRedacted && DateTime.UtcNow <= CreatedAt.ToUniversalTime().AddMinutes(30);
}

public sealed class BlogCommentListVm
{
    public IReadOnlyList<BlogCommentVm> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasResults => Items.Count > 0;
}

public sealed class CreateBlogCommentFormVm
{
    public Guid? ParentCommentId { get; set; }
    public string? Slug { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 2)]
    public string Content { get; set; } = "";
}

public sealed class EditBlogCommentFormVm
{
    public string? Slug { get; set; }
    public string? RowVersion { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 2)]
    public string Content { get; set; } = "";
}

public sealed class FollowStateResponse
{
    public bool IsFollowing { get; init; }
}

public static class BlogCommentMapper
{
    public static BlogCommentListVm ToVm(BlogCommentPageResponse response)
    {
        var all = response.Items.Select(ToItem).ToList();
        var byParent = all
            .Where(c => c.ParentCommentId.HasValue)
            .GroupBy(c => c.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BlogCommentVm>)g.OrderBy(c => c.CreatedAt).ToList());

        foreach (var item in all)
        {
            if (byParent.TryGetValue(item.Id, out var replies))
            {
                item.Replies = replies;
            }
        }

        return new BlogCommentListVm
        {
            Items = all.Where(c => c.ParentCommentId is null).OrderBy(c => c.CreatedAt).ToList(),
            Page = response.Page > 0 ? response.Page : response.PageNumber,
            PageNumber = response.PageNumber > 0 ? response.PageNumber : response.Page,
            PageSize = response.PageSize,
            TotalCount = response.TotalCount,
            HasPreviousPage = response.HasPreviousPage,
            HasNextPage = response.HasNextPage,
        };
    }

    public static CreateBlogCommentBody ToRequest(CreateBlogCommentFormVm form)
        => new(form.Content.Trim(), form.ParentCommentId);

    public static EditBlogCommentBody ToRequest(EditBlogCommentFormVm form)
        => new(DecodeRowVersion(form.RowVersion), form.Content.Trim());

    public static DeleteBlogCommentBody ToDeleteRequest(string? rowVersion)
        => new(DecodeRowVersion(rowVersion));

    private static BlogCommentVm ToItem(BlogCommentResponse item) => new()
    {
        Id = item.Id,
        BlogId = item.BlogId,
        ParentCommentId = item.ParentCommentId,
        UserId = item.UserId,
        Content = item.Content,
        IsContentRedacted = item.IsContentRedacted,
        LikeCount = item.LikeCount,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        RowVersion = item.RowVersion,
    };

    private static byte[] DecodeRowVersion(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion)) return [];

        try { return Convert.FromBase64String(rowVersion); }
        catch (FormatException) { return []; }
    }
}
