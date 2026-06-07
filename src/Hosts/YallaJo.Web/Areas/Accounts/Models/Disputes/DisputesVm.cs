using System.ComponentModel.DataAnnotations;
using YallaJo.Web.Areas.Accounts.Models.Payments;

namespace YallaJo.Web.Areas.Accounts.Models.Disputes;

/// <summary>
/// §3.8 Disputes — standalone list page (route <c>/accounts/disputes</c>).
/// Lists the current user's disputes (<c>GET /disputes/my</c>) and hosts the
/// open-dispute form (<c>POST /disputes</c>, against one of the user's payments).
/// </summary>
public sealed class DisputesVm
{
    public IReadOnlyList<DisputeRowVm> Disputes { get; init; } = [];

    /// <summary>Payments the user can dispute (drives the open-dispute select).</summary>
    public IReadOnlyList<DisputablePaymentVm> DisputablePayments { get; init; } = [];

    /// <summary>The open-dispute form (re-bound on validation failure).</summary>
    public OpenDisputeFormVm Form { get; set; } = new();

    // ── Paging (D1): pager driven only by HasPrevious/HasNext, never TotalPages ──
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public bool HasDisputes => Disputes.Count > 0;
    public bool CanOpenDispute => DisputablePayments.Count > 0;
}

public sealed record DisputeRowVm(
    Guid Id,
    Guid PaymentId,
    string Reason,
    string Description,
    string Status,
    string? Resolution,
    string? ResolutionNotes,
    DateTime CreatedAt,
    DateTime? ResolvedAt)
{
    /// <summary>Bootstrap badge class for the dispute status (colour + text, A11Y).</summary>
    public string StatusBadgeClass => Status switch
    {
        "Open" => "bg-warning bg-opacity-10 text-warning",
        "UnderReview" => "bg-info bg-opacity-10 text-info",
        "Resolved" => "bg-success bg-opacity-10 text-success",
        "Escalated" => "bg-danger bg-opacity-10 text-danger",
        "Rejected" => "bg-danger bg-opacity-10 text-danger",
        _ => "bg-secondary bg-opacity-10 text-secondary",
    };

    public bool IsResolved => ResolvedAt is not null
        || string.Equals(Status, "Resolved", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A payment the user owns that can be the target of a new dispute.</summary>
public sealed record DisputablePaymentVm(
    Guid PaymentId,
    Guid? BookingId,
    decimal Amount,
    string Currency,
    DateTime CreatedAt)
{
    public string Label =>
        $"{Amount.ToString("0.000")} {Currency} · {CreatedAt:d MMM yyyy}";
}

public sealed class OpenDisputeFormVm
{
    [Required(ErrorMessage = "Please choose the payment you want to dispute.")]
    [Display(Name = "Payment")]
    public Guid PaymentId { get; set; }

    [Required(ErrorMessage = "Please provide a reason.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Reason must be between 3 and 120 characters.")]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the issue.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be at least 10 characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;
}
