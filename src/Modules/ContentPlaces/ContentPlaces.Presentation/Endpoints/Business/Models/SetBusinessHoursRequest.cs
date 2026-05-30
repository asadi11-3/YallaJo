namespace ContentPlaces.Presentation.Endpoints.Business.Models;

public sealed record BusinessHoursEntryRequest(
    int DayOfWeek,
    string? OpenTime,
    string? CloseTime,
    bool IsClosed);

public sealed record SetBusinessHoursRequest(List<BusinessHoursEntryRequest> Hours);
