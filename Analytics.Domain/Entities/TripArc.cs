using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

/// <summary>
/// Pre-defined trip arc template for multi-day itinerary planning.
/// Example: "Full Jordan: Amman→Dead Sea→Petra→Wadi Rum→Aqaba"
/// </summary>
public sealed class TripArc : BaseEntity, IAggregateRoot
{
    private TripArc() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>
    /// JSON array of day clusters, e.g. [{"day":1,"placeId":"...","name":"Amman","lat":31.9454,"lng":35.9284},...]
    /// Each entry represents a geographic cluster for that day in the arc.
    /// </summary>
    public string DayClustersJson { get; private set; } = "[]";

    /// <summary>Minimum recommended days for this arc.</summary>
    public int MinDays { get; private set; }

    /// <summary>Maximum recommended days for this arc.</summary>
    public int MaxDays { get; private set; }

    /// <summary>Comma-separated interest tags, e.g. "adventure,culture,relaxation"</summary>
    public string? InterestTags { get; private set; }
    public bool IsActive { get; private set; }

    public static TripArc Create(
        string name, string? description, string dayClustersJson, int minDays, int maxDays, string? interestTags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(minDays, 1);
        if (maxDays < minDays)
            throw new ArgumentException("MaxDays must be >= MinDays", nameof(maxDays));

        return new TripArc
        {
            Name = name,
            Description = description,
            DayClustersJson = dayClustersJson,
            MinDays = minDays,
            MaxDays = maxDays,
            InterestTags = interestTags,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void Update(string name, string? description, string dayClustersJson, int minDays, int maxDays, string? interestTags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(minDays, 1);
        if (maxDays < minDays)
            throw new ArgumentException("MaxDays must be >= MinDays", nameof(maxDays));

        Name = name;
        Description = description;
        DayClustersJson = dayClustersJson;
        MinDays = minDays;
        MaxDays = maxDays;
        InterestTags = interestTags;
    }
}
