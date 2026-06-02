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

    [Display(Name = "Place ID (optional)")]
    public Guid? PlaceId { get; set; }

    [Display(Name = "Read time (minutes)")]
    [Range(0, 1000)]
    public int? ReadTimeMinutes { get; set; }

    // ── Read-only context for the view (status-aware action buttons) ────────────
    public string StatusLabel { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
}
