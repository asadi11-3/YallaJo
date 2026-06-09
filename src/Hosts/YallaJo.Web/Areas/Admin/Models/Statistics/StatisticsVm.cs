namespace YallaJo.Web.Areas.Admin.Models.Statistics;

public sealed class StatisticsVm
{
    public IReadOnlyList<InteractionTypeCountVm> TypeBreakdown { get; init; } = [];
    public IReadOnlyList<InteractionRowVm> Recent { get; init; } = [];
    public int TotalShown { get; init; }
    public string? UserLookupId { get; init; }
    public bool IsUserLookup { get; init; }
}

public sealed class InteractionTypeCountVm
{
    public string InteractionType { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class InteractionRowVm
{
    public long Id { get; init; }
    public Guid? UserId { get; init; }
    public string InteractionType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? UserAgent { get; init; }

    /// <summary>
    /// Human-readable name for <see cref="EntityId"/> when it could be resolved
    /// (Tour/Place/Blog/Guide). Null when the entity type has no clean lookup
    /// (Business/Creator) or resolution failed; the view then shows the localized
    /// entity-type label instead of a raw GUID (UI-UX rule F10).
    /// </summary>
    public string? EntityName { get; init; }

    /// <summary>
    /// Human-readable name (email) for <see cref="UserId"/> when resolved.
    /// Null for anonymous interactions or when resolution failed; the view shows a
    /// localized "Anonymous"/"Unknown user" label, never a raw GUID (F10).
    /// </summary>
    public string? UserName { get; init; }
}
