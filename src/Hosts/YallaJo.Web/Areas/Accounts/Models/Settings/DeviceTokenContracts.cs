namespace YallaJo.Web.Areas.Accounts.Models.Settings;

/// <summary>
/// Wire contract for <c>GET /api/v1/devices/tokens</c> (Messaging <c>DeviceTokenDto</c>) —
/// the current user's registered push-notification device tokens (§3.10 Devices tab).
/// </summary>
public sealed class DeviceTokenResponse
{
    public Guid Id { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public DateTime LastSeenAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Wire contract for <c>POST /api/v1/devices/token</c> (Messaging
/// <c>RegisterDeviceTokenRequest</c>). <c>Platform</c> is the backend
/// <c>DevicePlatform</c> enum name (e.g. "Web", "Android", "iOS").
/// </summary>
public sealed record RegisterDeviceTokenApiRequest(string DeviceId, string Platform, string Token);

/// <summary>Row VM for the Devices tab.</summary>
public sealed record DeviceTokenRowVm(
    Guid Id,
    string DeviceId,
    string Platform,
    DateTime LastSeenAt,
    DateTime CreatedAt);
