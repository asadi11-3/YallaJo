using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Security.Application.Queries.GetAuditLogs
{
    public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string? IpAddress,
    DateTime OccurredAt);
}
