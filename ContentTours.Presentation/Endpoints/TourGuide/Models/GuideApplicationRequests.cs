namespace ContentTours.Presentation.Endpoints.TourGuide.Models;

internal sealed record ApplyForTourRequest(
    string Message,
    string RelevantExperience,
    decimal? ProposedBasePrice = null,
    string? ProposedScheduleJson = null);

internal sealed record RejectGuideApplicationRequest(string Reason);

internal sealed record UpdateGuideAvatarRequest(string AvatarUrl);

internal sealed record UpdateGuideCoverImageRequest(string? CoverImageUrl);

internal sealed record SuspendTourGuideRequest(string Reason);
