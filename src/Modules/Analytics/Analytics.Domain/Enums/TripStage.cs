namespace Analytics.Domain.Enums;

public enum TripStage : byte
{
    None = 0,
    Pre = 1,
    JustLanded = 2,
    MidTrip = 3,
    LastDay = 4,
    PostTrip = 5
}
