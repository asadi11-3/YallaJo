namespace YallaJo.Web.Areas.Admin.Models.Disputes;

/// <summary>
/// Mirrors the API's DisputeStatusCountsDto (GET /api/v1/disputes/admin/status-counts).
/// Counts cover the full dispute table, including statuses the open-only admin list omits.
/// </summary>
public sealed class DisputeStatusCountsResponse
{
    public int Open { get; set; }
    public int UnderReview { get; set; }
    public int Resolved { get; set; }
    public int Escalated { get; set; }
    public int Closed { get; set; }
}

public sealed class DisputeResponse
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}
