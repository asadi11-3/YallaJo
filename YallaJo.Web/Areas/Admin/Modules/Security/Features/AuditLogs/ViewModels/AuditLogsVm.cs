namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;

public sealed class AuditLogListVm
{
    public IReadOnlyList<AuditLogRowVm> Logs        { get; init; } = [];
    public int   Page        { get; init; } = 1;
    public int   TotalCount  { get; init; }
    public bool  HasPrevious { get; init; }
    public bool  HasNext     { get; init; }
    public Guid? FilterUserId { get; init; }
}

public sealed class AuditLogRowVm
{
    public Guid     Id         { get; init; }
    public Guid?    UserId     { get; init; }
    public string   Action     { get; init; } = string.Empty;
    public string?  IpAddress  { get; init; }
    public DateTime OccurredAt { get; init; }
}
