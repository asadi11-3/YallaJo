namespace Social.Infrastructure.BackgroundServices;

/// <summary>Configuration for the daily Bayesian rating recalculation background service.</summary>
public sealed class RatingRecalculationOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Social:BackgroundServices:RatingRecalculation";

    /// <summary>Enable/disable the service. Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Time of day (UTC) to run the daily recalculation. Default: 03:00.</summary>
    public TimeSpan TargetTimeUtc { get; set; } = new TimeSpan(3, 0, 0);

    /// <summary>
    /// Bayesian global average rating (C × GlobalAvg term). Default: 3.0.
    /// Tune once enough platform data is available.
    /// </summary>
    public decimal GlobalAverageRating { get; set; } = 3.0m;

    /// <summary>
    /// Bayesian confidence constant C. Default: 10 (≈ 10 phantom reviews at GlobalAvg).
    /// Higher = slower ramp-up for new targets.
    /// </summary>
    public decimal ConfidenceConstant { get; set; } = 10m;

    /// <summary>Minimum actual review count before BayesianScore is exposed via the API. Default: 3.</summary>
    public int MinReviewsToShow { get; set; } = 3;
}
