using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Blogs;

public sealed class EditBlogVm
{
    public Guid Id { get; set; }

    /// <summary>Base64-encoded RowVersion round-tripped through a hidden form field.</summary>
    public string RowVersion { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    public string? Summary { get; set; }

    [Display(Name = "Meta title")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta description")]
    public string? MetaDescription { get; set; }

    /// <summary>
    /// Optional related place. Rendered as a name dropdown (F10: never a raw GUID
    /// textbox); the user reads the place name, the option value carries the id.
    /// </summary>
    public Guid? PlaceId { get; set; }

    /// <summary>Selectable places (name shown, id submitted). Populated by the facade.</summary>
    public IReadOnlyList<PlaceOptionVm> PlaceOptions { get; set; } = [];

    [Display(Name = "Read time (minutes)")]
    [Range(0, 1000)]
    public int? ReadTimeMinutes { get; set; }

    // ── Read-only context for the view (status-aware action buttons) ────────────
    public string StatusLabel { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public string LanguageCode { get; init; } = string.Empty;

    // ── Tour linking (Phase 4) ───────────────────────────────────────────────────
    public IReadOnlyList<LinkedTourVm> LinkedTours { get; init; } = [];
    public IReadOnlyList<TourOptionVm> AvailableTours { get; init; } = [];
}

/// <summary>A tour currently linked to the blog (title resolved from the lookup; falls back to id).</summary>
public sealed class LinkedTourVm
{
    public Guid TourId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>A selectable published tour for the link dropdown.</summary>
public sealed class TourOptionVm
{
    public Guid TourId { get; init; }
    public string Name { get; init; } = string.Empty;
}
