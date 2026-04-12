namespace YallaJo.Web.Areas.Auth.Features.Sessions.ViewModels;

public sealed class SessionsVm
{
    public IReadOnlyList<SessionItemVm> Sessions { get; init; } = [];
}

public sealed class SessionItemVm
{
    public Guid     SessionId  { get; init; }
    public Guid     DeviceId   { get; init; }
    public string   DeviceName { get; init; } = "Unknown Device";
    public string?  UserAgent  { get; init; }
    public string?  IpAddress  { get; init; }
    public DateTime CreatedAt  { get; init; }
    public DateTime ExpiresAt  { get; init; }
    public bool     IsCurrent  { get; init; }
}
