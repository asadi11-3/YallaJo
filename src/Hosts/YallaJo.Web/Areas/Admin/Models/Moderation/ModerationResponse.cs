namespace YallaJo.Web.Areas.Admin.Models.Moderation;

public sealed class ModerationLogPageResponse
{
    public IReadOnlyList<ModerationLogItemResponse> Items { get; set; } = [];
    public Guid? NextCursor { get; set; }
}

public sealed class ModerationLogItemResponse
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
