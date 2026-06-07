namespace YallaJo.Web.Areas.Provider.Models.TourAvailability;

// ── Responses (from API) ───────────────────────────────────────────────────────

/// <summary>Mirrors ManageAvailabilitySlotDto from GET /api/v1/booking/availability/{tourId}/manage.</summary>
public sealed class ManageAvailabilitySlotResponse
{
    public Guid Id { get; init; }
    public Guid TourId { get; init; }
    public Guid TourGuideId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int MaxCapacity { get; init; }
    public int AvailableCount { get; init; }
    public bool IsActive { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

/// <summary>Mirrors AvailabilitySlotDto from POST /api/v1/booking/availability/slots.</summary>
public sealed class CreateAvailabilitySlotResponse
{
    public Guid Id { get; init; }
    public Guid TourId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int MaxCapacity { get; init; }
    public int AvailableCount { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

// ── Requests (to API) ──────────────────────────────────────────────────────────

public sealed record CreateAvailabilitySlotApiRequest(
    Guid TourId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity);

public sealed record UpdateAvailabilitySlotApiRequest(
    int MaxCapacity,
    string RowVersion);

// POST /api/v1/booking/availability/slots/bulk
// Recurrence: "Daily" | "Weekly" | "Custom". DaysOfWeek (full names, e.g. "Monday") required when Custom.
public sealed record CreateBulkAvailabilitySlotsApiRequest(
    Guid TourId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Recurrence,
    IReadOnlyList<string>? DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    bool? SkipExisting);
