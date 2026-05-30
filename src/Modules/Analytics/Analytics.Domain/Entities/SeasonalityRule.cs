using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class SeasonalityRule : BaseEntity, IAggregateRoot
{
    private SeasonalityRule() { } // EF Core

    public Guid PlaceId { get; private set; }
    public int MonthStart { get; private set; }
    public int MonthEnd { get; private set; }
    public decimal Multiplier { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public static SeasonalityRule Create(
        Guid placeId, int monthStart, int monthEnd, decimal multiplier, string? description = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(monthStart, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(monthStart, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(monthEnd, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(monthEnd, 12);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(multiplier, 0m);

        return new SeasonalityRule
        {
            PlaceId = placeId,
            MonthStart = monthStart,
            MonthEnd = monthEnd,
            Multiplier = multiplier,
            Description = description,
            IsActive = true
        };
    }

    public bool AppliesToMonth(int month)
    {
        if (MonthStart <= MonthEnd)
            return month >= MonthStart && month <= MonthEnd;
        // Wrapping range: e.g., Nov(11)–Mar(3)
        return month >= MonthStart || month <= MonthEnd;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void Update(int monthStart, int monthEnd, decimal multiplier, string? description)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(monthStart, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(monthStart, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(monthEnd, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(monthEnd, 12);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(multiplier, 0m);

        MonthStart = monthStart;
        MonthEnd = monthEnd;
        Multiplier = multiplier;
        Description = description;
    }
}
