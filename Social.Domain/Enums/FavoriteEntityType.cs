namespace Social.Domain.Enums;

/// <summary>Entity types that can be added to a user's favorites list.</summary>
public enum FavoriteEntityType : byte
{
    Tour      = 0,
    Place     = 1,
    Business  = 2,
    Blog      = 3,
    TourGuide = 4,
}
