using ContentTours.Application.Commands.GuideAvailabilityBlock.Create;

namespace ContentTours.Presentation.Endpoints.TourGuide.Models;

internal sealed record ApplyForTourRequest(
    string Message,
    string RelevantExperience,
    decimal? ProposedBasePrice = null,
    string? ProposedScheduleJson = null);

internal sealed record RejectGuideApplicationRequest(string Reason);

internal sealed record UpdateGuideAvatarRequest(string AvatarUrl);

internal sealed record SuspendTourGuideRequest(string Reason);

public sealed record CreateGuideAvailabilityBlockRequest(DateOnly StartDate, DateOnly EndDate, string? Reason)
{
    public CreateGuideAvailabilityBlockCommand ToCommand() => new(StartDate, EndDate, Reason);
}

public sealed record GuideDashboardPageRequest(int Page = 1, int PageSize = 20);

public sealed record GuideBookingTrendsRequest(string Granularity = "monthly", int Months = 6);

public sealed record GuidePopularToursRequest(int Limit = 10);
