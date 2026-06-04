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
}
