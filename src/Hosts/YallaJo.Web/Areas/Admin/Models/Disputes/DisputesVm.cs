namespace YallaJo.Web.Areas.Admin.Models.Disputes;

public sealed class DisputesVm
{
    public IReadOnlyList<DisputeRowVm> Disputes { get; set; } = [];
    public string? StatusFilter { get; set; }
}

public sealed class DisputeRowVm
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanReview { get; set; }
    public bool CanResolve { get; set; }
    public bool CanEscalate { get; set; }
}
