namespace Social.Domain.Enums;

/// <summary>Entities that can receive a user review.</summary>
public enum ReviewTargetType : byte
{
    Tour      = 0,
    Place     = 1,
    Business  = 2,
    TourGuide = 3,
}
