namespace YallaJo.Web.Areas.Provider.Models.TourGuides;

// GET /api/v1/tours/{id}/guides
public sealed class TourGuideResponse
{
    public Guid TourGuideId { get; init; }
    public bool IsPrimary { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
}

// POST /api/v1/tours/{id}/guides
public sealed record AssignTourGuideApiRequest(Guid TourGuideUserId, bool IsPrimary);
