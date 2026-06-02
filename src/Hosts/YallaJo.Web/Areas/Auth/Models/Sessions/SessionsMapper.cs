using YallaJo.Web.Areas.Auth.Models.Sessions;

namespace YallaJo.Web.Areas.Auth.Models.Sessions;

public static class SessionsMapper
{
    public static SessionItemVm ToVm(SessionItemResponse r) => new()
    {
        SessionId  = r.SessionId,
        DeviceId   = r.DeviceId,
        DeviceName = r.DeviceName ?? "Unknown Device",
        UserAgent  = r.UserAgent,
        IpAddress  = r.IpAddress,
        CreatedAt  = r.CreatedAt,
        ExpiresAt  = r.ExpiresAt,
        IsCurrent  = r.IsCurrent,
    };
}
