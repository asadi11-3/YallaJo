namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels
{
    public sealed class AuditLogListVm
    {
        public IReadOnlyList<AuditLogRowVm> Logs { get; init; } = [];
        public int Page { get; init; } = 1;
        public int TotalCount { get; init; }
        public bool HasPrevious { get; init; }
        public bool HasNext { get; init; }
        public Guid? FilterUserId { get; init; }
    }
}
