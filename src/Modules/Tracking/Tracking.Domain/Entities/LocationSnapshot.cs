using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Tracking.Domain.Entities;

public sealed class LocationSnapshot : BaseEntity 
{
    private LocationSnapshot() { } // EF Core

    public Guid SessionId { get; private set; }
    public Location Location { get; private set; } = null!;
    public double Accuracy { get; private set; }
    public double? Speed { get; private set; }
    public double? Heading { get; private set; }
    public double? Altitude { get; private set; }
    public DateTime CapturedAt { get; private set; }

    public LiveTrackingSession LiveTrackingSession { get; private set; } = null!;

    internal static LocationSnapshot Create(
        Guid sessionId,
        Location location,
        double accuracy,
        double? speed,
        double? heading,
        double? altitude,
        DateTime capturedAt)
        => new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            Location = location,
            Accuracy = accuracy,
            Speed = speed,
            Heading = heading,
            Altitude = altitude,
            CapturedAt = capturedAt
        };
}
