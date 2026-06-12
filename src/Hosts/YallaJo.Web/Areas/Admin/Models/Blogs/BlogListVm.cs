namespace YallaJo.Web.Areas.Admin.Models.Blogs;

/// <summary>Which admin list tab is being shown (each backed by a different endpoint).</summary>
public enum BlogAdminTab
{
    Published = 0,
    Queue = 1,
    Deleted = 2,
    Drafts = 3,
}

/// <summary>Top-level view model for the admin blog list (<c>/admin/blogs</c>).</summary>
public sealed class BlogListVm
{
    public BlogAdminTab Tab { get; init; } = BlogAdminTab.Published;
    public IReadOnlyList<BlogRowVm> Items { get; init; } = [];

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public string? Search { get; init; }
    public bool? IsFeatured { get; init; }

    public bool HasItems => Items.Count > 0;
    public bool IsDeletedTab => Tab == BlogAdminTab.Deleted;

    /// <summary>
    /// Modal-CRUD state (F8 §4.7): when true the "new draft" modal renders
    /// server-side open so deep links and no-JS still work (PE1).
    /// </summary>
    public bool CreateOpen { get; set; }

    /// <summary>Bound "new draft" form values (posted with the <c>Create.</c> prefix).</summary>
    public CreateBlogVm Create { get; set; } = new();
}

/// <summary>A single row in the admin blog list. No image field (managed separately).</summary>
public sealed class BlogRowVm
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;

    /// <summary>Status label. For the Published tab this is always "Published".</summary>
    public string StatusLabel { get; init; } = string.Empty;
    public DateTime? PublishedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    public int ViewCount { get; init; }
    public bool IsFeatured { get; init; }
    public Guid? PlaceId { get; init; }

    /// <summary>Base64 optimistic-concurrency token (deleted tab only; required by Restore).</summary>
    public string? RowVersion { get; init; }
}
