using System.Text.Json;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Validators;

/// <summary>
/// Validates <see cref="CreatorPost.TypeSpecificDataJson"/> against schemas
/// defined per <see cref="CreatorPostType"/>. Wave 8.
/// </summary>
public static class CreatorPostTypeValidator
{
    /// <summary>
    /// Validates the type-specific JSON payload against the schema for the given post type.
    /// Returns <see cref="Result.Success()"/> if valid, or a failure with a descriptive error.
    /// </summary>
    public static Result Validate(CreatorPostType postType, string? typeSpecificDataJson)
    {
        if (string.IsNullOrWhiteSpace(typeSpecificDataJson))
            return Result.Failure(TypeValidationErrors.TypeSpecificDataRequired);

        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(typeSpecificDataJson);
            root = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return Result.Failure(TypeValidationErrors.InvalidJson);
        }

        if (root.ValueKind != JsonValueKind.Object)
            return Result.Failure(TypeValidationErrors.InvalidJson);

        return postType switch
        {
            CreatorPostType.Video => ValidateVideo(root),
            CreatorPostType.PhotoStory => ValidatePhotoStory(root),
            CreatorPostType.LongReview => ValidateLongReview(root),
            CreatorPostType.Itinerary => ValidateItinerary(root),
            _ => Result.Failure(TypeValidationErrors.UnsupportedPostType)
        };
    }

    // ────────── Video ──────────

    private static Result ValidateVideo(JsonElement root)
    {
        if (!TryGetNonEmptyString(root, "videoProvider", out var provider))
            return Result.Failure(TypeValidationErrors.VideoProviderRequired);

        var validProviders = new[] { "YouTube", "Vimeo", "Hosted" };
        if (!validProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(TypeValidationErrors.VideoProviderInvalid);

        if (!TryGetNonEmptyString(root, "videoUrl", out _))
            return Result.Failure(TypeValidationErrors.VideoUrlRequired);

        if (root.TryGetProperty("durationSeconds", out var duration))
        {
            if (duration.ValueKind != JsonValueKind.Number || !duration.TryGetInt32(out var secs) || secs <= 0)
                return Result.Failure(TypeValidationErrors.VideoDurationInvalid);
        }

        return Result.Success();
    }

    // ────────── PhotoStory ──────────

    private static Result ValidatePhotoStory(JsonElement root)
    {
        if (!root.TryGetProperty("images", out var images) || images.ValueKind != JsonValueKind.Array)
            return Result.Failure(TypeValidationErrors.PhotoStoryImagesRequired);

        var imageCount = images.GetArrayLength();
        if (imageCount < 3)
            return Result.Failure(TypeValidationErrors.PhotoStoryTooFewImages);
        if (imageCount > 30)
            return Result.Failure(TypeValidationErrors.PhotoStoryTooManyImages);

        var idx = 0;
        foreach (var img in images.EnumerateArray())
        {
            if (!TryGetNonEmptyString(img, "attachmentId", out _))
                return Result.Failure(TypeValidationErrors.PhotoStoryImageMissingAttachmentId(idx));

            idx++;
        }

        if (root.TryGetProperty("coverImageIndex", out var coverIdx))
        {
            if (coverIdx.ValueKind != JsonValueKind.Number || !coverIdx.TryGetInt32(out var ci) || ci < 0 || ci >= imageCount)
                return Result.Failure(TypeValidationErrors.PhotoStoryCoverIndexOutOfRange);
        }

        return Result.Success();
    }

    // ────────── LongReview ──────────

    private static Result ValidateLongReview(JsonElement root)
    {
        if (!TryGetNonEmptyString(root, "reviewedEntityType", out var entityType))
            return Result.Failure(TypeValidationErrors.LongReviewEntityTypeRequired);

        var validEntityTypes = new[] { "Tour", "Place", "Business" };
        if (!validEntityTypes.Contains(entityType, StringComparer.OrdinalIgnoreCase))
            return Result.Failure(TypeValidationErrors.LongReviewEntityTypeInvalid);

        if (!TryGetNonEmptyString(root, "reviewedEntityId", out _))
            return Result.Failure(TypeValidationErrors.LongReviewEntityIdRequired);

        if (!root.TryGetProperty("overallRating", out var rating) || rating.ValueKind != JsonValueKind.Number)
            return Result.Failure(TypeValidationErrors.LongReviewRatingRequired);

        if (!rating.TryGetDouble(out var ratingValue) || ratingValue < 1.0 || ratingValue > 5.0)
            return Result.Failure(TypeValidationErrors.LongReviewRatingOutOfRange);

        // Half-points only: 1.0, 1.5, 2.0 ... 5.0
        if (ratingValue % 0.5 != 0)
            return Result.Failure(TypeValidationErrors.LongReviewRatingHalfPointsOnly);

        return Result.Success();
    }

    // ────────── Itinerary ──────────

    private static Result ValidateItinerary(JsonElement root)
    {
        if (!root.TryGetProperty("durationDays", out var durationDays) || durationDays.ValueKind != JsonValueKind.Number)
            return Result.Failure(TypeValidationErrors.ItineraryDurationDaysRequired);

        if (!durationDays.TryGetInt32(out var days) || days < 1 || days > 90)
            return Result.Failure(TypeValidationErrors.ItineraryDurationDaysOutOfRange);

        if (!root.TryGetProperty("days", out var daysArray) || daysArray.ValueKind != JsonValueKind.Array)
            return Result.Failure(TypeValidationErrors.ItineraryDaysArrayRequired);

        var dayCount = daysArray.GetArrayLength();
        if (dayCount == 0)
            return Result.Failure(TypeValidationErrors.ItineraryDaysArrayRequired);

        if (dayCount != days)
            return Result.Failure(TypeValidationErrors.ItineraryDayCountMismatch);

        var dayIdx = 0;
        foreach (var day in daysArray.EnumerateArray())
        {
            if (!day.TryGetProperty("dayNumber", out var dn) || dn.ValueKind != JsonValueKind.Number)
                return Result.Failure(TypeValidationErrors.ItineraryDayMissingNumber(dayIdx));

            if (!day.TryGetProperty("activities", out var activities) || activities.ValueKind != JsonValueKind.Array)
                return Result.Failure(TypeValidationErrors.ItineraryDayMissingActivities(dayIdx));

            if (activities.GetArrayLength() == 0)
                return Result.Failure(TypeValidationErrors.ItineraryDayMissingActivities(dayIdx));

            dayIdx++;
        }

        return Result.Success();
    }

    // ────────── Helpers ──────────

    private static bool TryGetNonEmptyString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var prop))
            return false;
        if (prop.ValueKind != JsonValueKind.String)
            return false;
        value = prop.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
