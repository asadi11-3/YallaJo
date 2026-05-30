using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

/// <summary>
/// Per-guide schedule for a specific tour offering.
/// Each guide can define their own availability independent of other guides.
/// </summary>
public sealed class GuideSchedule : BaseEntity
{
    private GuideSchedule()
    {
    }

    public Guid GuideTourOfferingId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public Guid TourId { get; private set; }

    /// <summary>0 = Sunday, 1 = Monday, ..., 6 = Saturday</summary>
    public byte DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static GuideSchedule Create(
        Guid guideTourOfferingId,
        Guid tourGuideId,
        Guid tourId,
        byte dayOfWeek,
        TimeOnly startTime,
        TimeOnly? endTime = null)
    {
        return new GuideSchedule
        {
            GuideTourOfferingId = guideTourOfferingId,
            TourGuideId = tourGuideId,
            TourId = tourId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = true
        };
    }

    public void Update(byte dayOfWeek, TimeOnly startTime, TimeOnly? endTime)
    {
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
