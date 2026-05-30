namespace Analytics.Infrastructure.BackgroundServices;

internal sealed class SuggestionBatchRefreshOptions
{
    public const string SectionName = "Analytics:SuggestionBatchRefresh";

    public int RefreshIntervalMinutes { get; set; } = 360;
    public int StartupDelaySeconds { get; set; } = 30;
    public int MaxBatchesPerCycle { get; set; } = 100;
}
