namespace Analytics.Domain.Enums;

public enum InteractionType : byte
{
    View = 0,
    Click = 1,
    Search = 2,
    AddToFavorite = 3,
    RemoveFromFavorite = 4,
    BookingStarted = 5,
    BookingCompleted = 6,
    BookingCancelled = 7,
    Share = 8,
    ReviewSubmitted = 9
}
