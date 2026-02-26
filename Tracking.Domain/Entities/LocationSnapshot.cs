using YallaJo.SharedKernel.Domain.Entities;

namespace Tracking.Domain.Entities;

public sealed class LocationSnapshot : BaseEntity
{
    private LocationSnapshot() { } // EF Core

    public Guid SessionId { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public double Accuracy { get; private set; }
    public double? Speed { get; private set; }
    public double? Heading { get; private set; }
    public double? Altitude { get; private set; }
    public DateTime CapturedAt { get; private set; }

    public LiveTrackingSession LiveTrackingSession { get; private set; } = null!;
}
