namespace YallaJo.Web.Areas.Admin.Models.Reports;

public sealed class ReportPageResponse
{
    public IReadOnlyList<ReportItemResponse> Items { get; set; } = [];
    public Guid? NextCursor { get; set; }
}

public sealed class ReportItemResponse
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Reason { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionAction { get; set; }
    public string? ResolutionNotes { get; set; }
}
