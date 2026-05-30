using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Responses;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Mappers;

internal static class AuditLogsMapper
{
    public static AuditLogRowVm ToRowVm(AuditLogItemResponse r) => new()
    {
        Id           = r.Id,
        UserId       = r.UserId,
        ActorUserId  = r.ActorUserId,
        Action       = r.Action,
        ResourceType = r.ResourceType,
        ResourceId   = r.ResourceId,
        IpAddress    = r.IpAddress,
        Reason       = r.Reason,
        Metadata     = r.Metadata,
        OccurredAt   = r.OccurredAt,
    };
}
