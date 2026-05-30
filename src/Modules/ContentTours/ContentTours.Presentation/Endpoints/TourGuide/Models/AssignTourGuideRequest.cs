namespace ContentTours.Presentation.Endpoints.TourGuide.Models;

public sealed record AssignTourGuideRequest(Guid TourGuideUserId, bool IsPrimary);
