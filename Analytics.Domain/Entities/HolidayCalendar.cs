using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class HolidayCalendar : BaseEntity, IAggregateRoot
{
    private HolidayCalendar() { } // EF Core

    public string HolidayName { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int Year { get; private set; }

    /// <summary>
    /// JSON rules, e.g. [{"entityKind":"Tour","filter":"familySuitable","multiplier":1.3},{"filter":"alcoholRelated","multiplier":0.5}]
    /// </summary>
    public string? BoostRulesJson { get; private set; }
    public bool IsActive { get; private set; }

    public static HolidayCalendar Create(
        string holidayName, DateOnly startDate, DateOnly endDate, int year, string? boostRulesJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holidayName);
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 2020);
        if (endDate < startDate)
            throw new ArgumentException("EndDate must be >= StartDate", nameof(endDate));

        return new HolidayCalendar
        {
            HolidayName = holidayName,
            StartDate = startDate,
            EndDate = endDate,
            Year = year,
            BoostRulesJson = boostRulesJson,
            IsActive = true
        };
    }

    public bool IsActiveOn(DateOnly date)
        => IsActive && date >= StartDate && date <= EndDate;

    public void Deactivate() => IsActive = false;

    public void Update(DateOnly startDate, DateOnly endDate, string? boostRulesJson)
    {
        if (endDate < startDate)
            throw new ArgumentException("EndDate must be >= StartDate", nameof(endDate));
        StartDate = startDate;
        EndDate = endDate;
        BoostRulesJson = boostRulesJson;
    }
}
