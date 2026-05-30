namespace Analytics.Domain.Enums;

public enum SuggestionContext : byte
{
    SimilarTours = 1,
    SimilarBusinesses = 2,
    SimilarHotels = 3,
    AddAMeal = 10,
    AddAnActivity = 11,
    WhereToStay = 12,
    ExploreNearby = 13,
    PersonalizedFeed = 20,
    BecauseYouViewed = 21,
    BecauseYouBooked = 22,
    PostBookingAddOn = 30,
    PostBookingFollowUp = 31
}
