using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourSchedule : BaseEntity
{
    private TourSchedule()
    {
    } // EF Core

    public Guid TourId { get; private set; }
    public byte DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Tour Tour { get; private set; } = default!;

    /// <summary>Factory method — the only way to create a schedule row.</summary>
    public static TourSchedule Create(
        Guid tourId,
        byte dayOfWeek,
        TimeOnly startTime,
        TimeOnly? endTime,
        bool isActive)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));
        if (dayOfWeek > 6)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "DayOfWeek must be 0–6.");
        if (endTime is { } e && e <= startTime)
            throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));

        return new TourSchedule
        {
            TourId    = tourId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime   = endTime,
            IsActive  = isActive,
        };
    }

    /// <summary>Updates all mutable schedule fields. Caller must re-run overlap validation.</summary>
    public void Update(byte dayOfWeek, TimeOnly startTime, TimeOnly? endTime, bool isActive)
    {
        if (dayOfWeek > 6)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "DayOfWeek must be 0–6.");
        if (endTime is { } e && e <= startTime)
            throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime   = endTime;
        IsActive  = isActive;
    }

    /// <summary>Marks the schedule inactive (reversible).</summary>
    public void Deactivate() => IsActive = false;
}
