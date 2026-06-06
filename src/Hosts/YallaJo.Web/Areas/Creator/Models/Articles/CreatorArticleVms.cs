using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Creator.Models.Articles;

/// <summary>The My Articles list page (paged, optional status filter).</summary>
public sealed class MyArticlesVm
{
    public IReadOnlyList<MyArticleRowVm> Articles { get; init; } = [];

    public bool HasArticles => Articles.Count > 0;

    /// <summary>Currently-applied status filter (a BlogStatus name) or null for all.</summary>
    public string? StatusFilter { get; init; }

    /// <summary>Selectable status options for the filter dropdown (BlogStatus names).</summary>
    public IReadOnlyList<string> StatusOptions { get; init; } = [];

    public ArticlePagerVm Pager { get; init; } = new();

    // ── Post-delete Undo (immediate restore only) ─────────────────────────────
    // Set after a successful delete so the list can offer a one-time "Undo".
    public Guid? UndoArticleId { get; init; }
    public string? UndoRowVersion { get; init; }
    public string? UndoTitle { get; init; }

    public bool ShowUndo => UndoArticleId is not null && !string.IsNullOrEmpty(UndoRowVersion);
}

/// <summary>
/// One row in the My Articles list. NOTE: the backend my-blogs summary does NOT
/// return per-article Status (BlogSummaryDto has none) or a real language code, so
/// this row deliberately omits a status badge — true status is shown in the editor
/// (sourced from admin-get). No fake status is rendered.
/// </summary>
public sealed class MyArticleRowVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
}

public sealed class ArticlePagerVm
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public string? StatusFilter { get; init; }
}

/// <summary>
/// Unified create/edit article editor view model. For edit, RowVersion + Status are
/// sourced from the admin-get prefetch (GET /api/v1/blogs/admin/{id}); the anonymous
/// GET /api/v1/blogs/{id} is never used because it lacks RowVersion.
/// </summary>
public sealed class ArticleEditorVm
{
    /// <summary>Null for a new draft; set when editing an existing article.</summary>
    public Guid? Id { get; set; }

    /// <summary>Base64 RowVersion from admin-get (required for update). Empty on create.</summary>
    public string RowVersion { get; set; } = string.Empty;

    /// <summary>True backend status ("Draft"/"Rejected"/"PendingReview"/…) for edit; null on create.</summary>
    public string? Status { get; set; }

    /// <summary>
    /// Article images section (CCD-5). Populated on GET edit (once a BlogId exists);
    /// null for a new (not-yet-created) article. Image management is a separate
    /// multipart flow — it never participates in the article text-save/RowVersion path.
    /// </summary>
    public Images.ArticleImagesVm? Images { get; set; }

    [Required(ErrorMessage = "Please enter a title.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be 3-200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write some content.")]
    public string Content { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Language")]
    public string SourceLanguageCode { get; set; } = "en";

    [StringLength(160)]
    [Display(Name = "URL slug")]
    public string? Slug { get; set; }

    [StringLength(500)]
    public string? Summary { get; set; }

    [StringLength(200)]
    [Display(Name = "Meta title")]
    public string? MetaTitle { get; set; }

    [StringLength(320)]
    [Display(Name = "Meta description")]
    public string? MetaDescription { get; set; }

    // ── View convenience ──────────────────────────────────────────────────────

    public bool IsNew => Id is null;

    /// <summary>Editing is allowed for Draft/Rejected (PendingReview/Published are not editor-editable here).</summary>
    public bool IsEditable =>
        Status is null
        || string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "Rejected", StringComparison.OrdinalIgnoreCase);

    /// <summary>Submit-for-review is allowed only from Draft or Rejected (backend rule).</summary>
    public bool CanSubmit =>
        Id is not null
        && (string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Status, "Rejected", StringComparison.OrdinalIgnoreCase));
}
