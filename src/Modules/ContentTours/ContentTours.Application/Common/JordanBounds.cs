namespace ContentTours.Application.Common;

/// <summary>
/// Jordan geographic bounding box used by Tour command handlers as a
/// warning-only sanity check on submitted Latitude/Longitude. Coordinates
/// outside this rectangle are logged at Warning level but never rejected
/// — Tour creation and updates are permitted anywhere in the world.
///
/// Values are <see cref="decimal"/> to match <c>Tour.Location.Latitude</c>
/// and <c>Tour.Location.Longitude</c> (no implicit decimal↔double conversion
/// at the call site).
/// </summary>
internal static class JordanBounds
{
    public const decimal MinLat = 29.18m;
    public const decimal MaxLat = 33.38m;
    public const decimal MinLng = 34.95m;
    public const decimal MaxLng = 39.30m;
}
