using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Creator.Models.Profile;

/// <summary>
/// View model for the creator profile page. Drives the editable form for an Active
/// profile and the read-only notice for Suspended/Deactivated. No DTOs leak into the
/// Razor view. Client-side limits mirror the backend FluentValidation rules; the
/// server remains the source of truth (errors surfaced via ApplyValidationErrors).
/// </summary>
public sealed class CreatorProfileVm
{
    /// <summary>Raw backend status ("Active" | "Suspended" | "Deactivated"); null when no profile.</summary>
    public string? Status { get; set; }

    public string? TrustTier { get; set; }
    public string CurrentSlug { get; set; } = string.Empty;

    // ── Read-only stats (display only) ────────────────────────────────────────
    public int ArticleCount { get; set; }
    public int FollowerCount { get; set; }

    // ── Editable fields (PUT /profile/mine) ───────────────────────────────────
    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(200, ErrorMessage = "Display name must be 200 characters or fewer.")]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Bio must be 2000 characters or fewer.")]
    public string? Bio { get; set; }

    /// <summary>New slug (optional). Lowercase alphanumeric segments separated by '-'.</summary>
    [StringLength(100, ErrorMessage = "Slug must be 100 characters or fewer.")]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug must be lowercase alphanumeric segments separated by '-'.")]
    [Display(Name = "Profile URL slug")]
    public string? NewSlug { get; set; }

    /// <summary>Current avatar URL (also editable inline on the profile form).</summary>
    [StringLength(500, ErrorMessage = "Avatar URL must be 500 characters or fewer.")]
    [Url(ErrorMessage = "Avatar URL must be a valid URL.")]
    [Display(Name = "Avatar URL")]
    public string? AvatarUrl { get; set; }

    // ── View convenience ──────────────────────────────────────────────────────

    public bool HasProfile => !string.IsNullOrWhiteSpace(Status);

    public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
}

/// <summary>View model for the dedicated avatar-URL update form (PUT /avatar).</summary>
public sealed class UpdateAvatarVm
{
    [Required(ErrorMessage = "Please enter an avatar URL.")]
    [StringLength(500, ErrorMessage = "Avatar URL must be 500 characters or fewer.")]
    [Url(ErrorMessage = "Avatar URL must be a valid URL.")]
    [Display(Name = "Avatar URL")]
    public string AvatarUrl { get; set; } = string.Empty;
}
