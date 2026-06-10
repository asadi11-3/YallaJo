namespace YallaJo.Web.Areas.Auth.Models.Sessions;

public sealed class SessionItemResponse
{
    public Guid      SessionId  { get; init; }
    public Guid      DeviceId   { get; init; }
    public string?   DeviceName { get; init; }
    public string?   UserAgent  { get; init; }
    public string?   IpAddress  { get; init; }
    public DateTime  CreatedAt  { get; init; }
    public DateTime  ExpiresAt  { get; init; }
    public bool      IsCurrent  { get; init; }
    public bool      IsTrusted  { get; init; }
}
