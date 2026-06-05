using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.Proposals;

/// <summary>
/// View model backing the guide "My Proposals" page.
/// </summary>
public sealed class ProposalsVm
{
    public IReadOnlyList<ProposalRowVm> Proposals { get; init; } = [];

    public CreateProposalFormVm Form { get; set; } = new();

    public bool HasProposals => Proposals.Count > 0;
}

/// <summary>
/// A single proposal row.
/// </summary>
public sealed record ProposalRowVm(
    Guid Id,
    string Title,
    string? ShortDescription,
    string Status,
    decimal BasePrice,
    string Currency,
    DateTime CreatedAt);

/// <summary>
/// Form used by the guide to create a tour proposal draft.
/// Validation mirrors the backend CreateTourProposalCommandValidator.
/// </summary>
public sealed class CreateProposalFormVm
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Short description")]
    public string? ShortDescription { get; set; }

    [Required]
    [Display(Name = "Place")]
    public Guid PlaceId { get; set; }

    [Range(1, 2880)]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 60;

    [Range(1, 100)]
    [Display(Name = "Max group size")]
    public int MaxGroupSize { get; set; } = 10;

    [Range(0.01, 1_000_000)]
    [Display(Name = "Base price")]
    public decimal BasePrice { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "JOD";

    [Display(Name = "Request exclusive rights")]
    public bool RequestExclusive { get; set; }
}
