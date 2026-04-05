using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Application.Queries.ListSessions
{
    public sealed record ActiveSessionDto(
     Guid SessionId,
     Guid DeviceId,
     string? DeviceName,
     string? UserAgent,
     string? IpAddress,
     DateTime CreatedAt,
     DateTime ExpiresAt,
     bool IsCurrent);
}
