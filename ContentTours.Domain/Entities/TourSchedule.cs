using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourSchedule : BaseEntity
{
    private TourSchedule() { } // EF Core

    public Guid TourId { get; private set; }
    public byte DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Tour Tour { get; private set; } = default!;
}
