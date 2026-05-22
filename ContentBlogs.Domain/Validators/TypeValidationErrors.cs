using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Validators;

/// <summary>Errors produced by <see cref="CreatorPostTypeValidator"/>. Wave 8.</summary>
public static class TypeValidationErrors
{
    // ── General ──
    public static readonly Error TypeSpecificDataRequired =
        new("CreatorPost.TypeSpecificDataRequired", "Type-specific data JSON is required.");
    public static readonly Error InvalidJson =
        new("CreatorPost.InvalidJson", "Type-specific data must be a valid JSON object.");
    public static readonly Error UnsupportedPostType =
        new("CreatorPost.UnsupportedPostType", "The specified post type is not supported for validation.");

    // ── Video ──
    public static readonly Error VideoProviderRequired =
        new("CreatorPost.Video.ProviderRequired", "Video provider is required (YouTube, Vimeo, or Hosted).");
    public static readonly Error VideoProviderInvalid =
        new("CreatorPost.Video.ProviderInvalid", "Video provider must be YouTube, Vimeo, or Hosted.");
    public static readonly Error VideoUrlRequired =
        new("CreatorPost.Video.UrlRequired", "Video URL is required.");
    public static readonly Error VideoDurationInvalid =
        new("CreatorPost.Video.DurationInvalid", "Video duration must be a positive integer (seconds).");

    // ── PhotoStory ──
    public static readonly Error PhotoStoryImagesRequired =
        new("CreatorPost.PhotoStory.ImagesRequired", "A photo story must include an images array.");
    public static readonly Error PhotoStoryTooFewImages =
        new("CreatorPost.PhotoStory.TooFewImages", "A photo story requires at least 3 images.");
    public static readonly Error PhotoStoryTooManyImages =
        new("CreatorPost.PhotoStory.TooManyImages", "A photo story can have at most 30 images.");
    public static readonly Error PhotoStoryCoverIndexOutOfRange =
        new("CreatorPost.PhotoStory.CoverIndexOutOfRange", "Cover image index is out of range.");

    public static Error PhotoStoryImageMissingAttachmentId(int index) =>
        new("CreatorPost.PhotoStory.MissingAttachmentId", $"Image at index {index} is missing an attachmentId.");

    // ── LongReview ──
    public static readonly Error LongReviewEntityTypeRequired =
        new("CreatorPost.LongReview.EntityTypeRequired", "Reviewed entity type is required (Tour, Place, or Business).");
    public static readonly Error LongReviewEntityTypeInvalid =
        new("CreatorPost.LongReview.EntityTypeInvalid", "Reviewed entity type must be Tour, Place, or Business.");
    public static readonly Error LongReviewEntityIdRequired =
        new("CreatorPost.LongReview.EntityIdRequired", "Reviewed entity ID is required.");
    public static readonly Error LongReviewRatingRequired =
        new("CreatorPost.LongReview.RatingRequired", "Overall rating is required.");
    public static readonly Error LongReviewRatingOutOfRange =
        new("CreatorPost.LongReview.RatingOutOfRange", "Overall rating must be between 1.0 and 5.0.");
    public static readonly Error LongReviewRatingHalfPointsOnly =
        new("CreatorPost.LongReview.HalfPointsOnly", "Overall rating must be in half-point increments (1.0, 1.5, 2.0, ...).");

    // ── Itinerary ──
    public static readonly Error ItineraryDurationDaysRequired =
        new("CreatorPost.Itinerary.DurationDaysRequired", "Duration days is required.");
    public static readonly Error ItineraryDurationDaysOutOfRange =
        new("CreatorPost.Itinerary.DurationDaysOutOfRange", "Duration days must be between 1 and 90.");
    public static readonly Error ItineraryDaysArrayRequired =
        new("CreatorPost.Itinerary.DaysArrayRequired", "Days array is required and must not be empty.");
    public static readonly Error ItineraryDayCountMismatch =
        new("CreatorPost.Itinerary.DayCountMismatch", "The number of day entries must match durationDays.");

    public static Error ItineraryDayMissingNumber(int index) =>
        new("CreatorPost.Itinerary.DayMissingNumber", $"Day at index {index} is missing a dayNumber.");
    public static Error ItineraryDayMissingActivities(int index) =>
        new("CreatorPost.Itinerary.DayMissingActivities", $"Day at index {index} must have at least one activity.");
}
