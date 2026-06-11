using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.Applications;

/// <summary>View model for the guide Applications page (status list + apply-to-run form).</summary>
public sealed class ApplicationsVm
{
    public IReadOnlyList<ApplicationRowVm> Applications { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ApplyForTourFormVm Form { get; set; } = new();

    /// <summary>Tours open for guide applications — server-rendered options for the picker (PE1).</summary>
    public IReadOnlyList<OpenTourOptionVm> OpenTours { get; set; } = [];

    public bool HasApplications => Applications.Count > 0;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>A single application row for display.</summary>
public sealed record ApplicationRowVm(
    Guid ApplicationId,
    Guid TourId,
    string TourTitle,
    string Status,
    string? Message,
    decimal? ProposedBasePrice,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? RejectionReason);

/// <summary>A selectable tour option for the apply-to-run picker (F10).</summary>
public sealed record OpenTourOptionVm(Guid TourId, string Label);

/// <summary>Form for applying to run a tour.</summary>
public sealed class ApplyForTourFormVm
{
    [Required(ErrorMessage = "Tour ID is required.")]
    [Display(Name = "Tour ID")]
    public Guid TourId { get; set; }

    [Required(ErrorMessage = "A message is required.")]
    [StringLength(2000, MinimumLength = 1)]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe your relevant experience.")]
    [StringLength(2000, MinimumLength = 1)]
    [Display(Name = "Relevant experience")]
    public string RelevantExperience { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    [Display(Name = "Proposed base price")]
    public decimal? ProposedBasePrice { get; set; }
}
