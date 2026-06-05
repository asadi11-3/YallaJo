using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Moderation;

public sealed class ModerationVm
{
    public IReadOnlyList<ModerationLogRowVm> Logs { get; set; } = [];
    public Guid? NextCursor { get; set; }
    public bool HasLogs => Logs.Count > 0;
}

public sealed class ModerationLogRowVm
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Action { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime ActionedAt { get; set; }
    public Guid? SourceReportId { get; set; }
}

/// <summary>Reportable entity types — mirrors Social.Domain.Enums.ReportableEntityType (byte values must match).</summary>
public enum ModerationEntityType : byte
{
    Review = 0,
    Tour = 1,
    Place = 2,
    Business = 3,
    Blog = 4,
    TourGuide = 5,
}

public sealed class WarnUserFormVm
{
    [Required]
    [Display(Name = "User ID")]
    public Guid UserId { get; set; }

    [Required]
    [Display(Name = "Entity type")]
    public ModerationEntityType EntityType { get; set; } = ModerationEntityType.Review;

    [Required]
    [Display(Name = "Entity ID")]
    public Guid EntityId { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = "";
}

public sealed class BanUserFormVm
{
    [Required]
    [Display(Name = "User ID")]
    public Guid UserId { get; set; }

    [Required]
    [Display(Name = "Entity type")]
    public ModerationEntityType EntityType { get; set; } = ModerationEntityType.Review;

    [Required]
    [Display(Name = "Entity ID")]
    public Guid EntityId { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = "";

    [DataType(DataType.DateTime)]
    [Display(Name = "Expires at (optional)")]
    public DateTime? ExpiresAt { get; set; }
}
