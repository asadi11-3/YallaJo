namespace YallaJo.Web.Areas.Provider.Models.TourSchedules;

public sealed class TourScheduleResponse
{
    public Guid Id { get; init; }
    public Guid TourId { get; init; }
    public byte DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string? EndTime { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class CreateTourScheduleResponse
{
    public int Created { get; init; }
    public int Skipped { get; init; }
}
