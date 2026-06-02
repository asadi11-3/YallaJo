namespace YallaJo.Web.Areas.Auth.Models.Devices;

/// <summary>
/// Minimal view model — the Devices page only exposes a "Trust" action.
/// Device list data comes from the Sessions page (each session carries DeviceId/DeviceName).
/// </summary>
public sealed class DevicesVm
{
    public Guid    DeviceId   { get; init; }
    public string? DeviceName { get; init; }
    public string? Message    { get; init; }
}
