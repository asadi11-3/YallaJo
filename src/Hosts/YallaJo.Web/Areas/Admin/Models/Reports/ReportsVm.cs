namespace YallaJo.Web.Areas.Admin.Models.Reports;

public sealed class ReportsVm
{
    public IReadOnlyList<ReportRowVm> Reports { get; set; } = [];
    public Guid? NextCursor { get; set; }
}

public sealed class ReportRowVm
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public string? ReporterEmail { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Reason { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public string? ResolutionAction { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool CanResolve { get; set; }
}
