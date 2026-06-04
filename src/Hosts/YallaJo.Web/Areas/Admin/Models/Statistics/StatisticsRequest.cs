namespace YallaJo.Web.Areas.Admin.Models.Statistics;

public sealed class StatisticsFilterRequest
{
    public string? InteractionType { get; init; }
    public string? EntityType { get; init; }
    public Guid? UserId { get; init; }
    public int PageSize { get; init; } = 50;
}
