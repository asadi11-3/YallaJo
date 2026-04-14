using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Responses;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Mappers;

internal static class AuditLogsMapper
{
    public static AuditLogRowVm ToRowVm(AuditLogItemResponse r) => new()
    {
        Id         = r.Id,
        UserId     = r.UserId,
        Action     = r.Action,
        IpAddress  = r.IpAddress,
        OccurredAt = r.OccurredAt,
    };
}
