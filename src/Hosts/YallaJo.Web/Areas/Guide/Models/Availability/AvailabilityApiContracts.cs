namespace YallaJo.Web.Areas.Guide.Models.Availability;

public sealed record CreateGuideAvailabilityBlockRequest(DateOnly StartDate, DateOnly EndDate, string? Reason);
