namespace YallaJo.Web.Areas.Guide.Models.Applications;

/// <summary>Paged result of the guide's own tour-run applications.</summary>
public sealed record GuideApplicationsResponse(
    IReadOnlyList<GuideApplicationListItemResponse> Items,
    int TotalCount);

/// <summary>A single guide application row as returned by the API.</summary>
public sealed record GuideApplicationListItemResponse(
    Guid ApplicationId,
    Guid TourId,
    string? TourTitle,
    string Status,
    string? Message,
    decimal? ProposedBasePrice,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? RejectionReason);

/// <summary>Request body for a guide applying to run a tour.</summary>
public sealed record ApplyForTourRequest(
    string Message,
    string RelevantExperience,
    decimal? ProposedBasePrice = null,
    string? ProposedScheduleJson = null);

/// <summary>Paged result of tours currently open for guide applications (F10 picker).</summary>
public sealed record OpenToursResponse(
    IReadOnlyList<OpenTourItemResponse> Items,
    int TotalCount);

/// <summary>A single open-for-applications tour as returned by the API.</summary>
public sealed record OpenTourItemResponse(
    Guid TourId,
    string Title,
    string? Slug,
    string? City,
    decimal BasePrice,
    string Currency);
