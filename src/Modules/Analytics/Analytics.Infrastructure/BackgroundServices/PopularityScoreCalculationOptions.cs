namespace Analytics.Infrastructure.BackgroundServices;

public sealed class PopularityScoreCalculationOptions
{
    public const string SectionName = "Analytics:BackgroundServices:PopularityCalculation";
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(2);
    public int BatchSize { get; set; } = 500;
}
