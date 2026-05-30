namespace Finance.Application.Commands.TriggerPayout;

/// <summary>
/// Configuration for Payout trigger thresholds (F-R7).
/// </summary>
public sealed class TriggerPayoutOptions
{
    public const string SectionName = "Finance:Payout";

    /// <summary>Minimum NetAmount for a payout to actually be created (else rolls over). Default 10.</summary>
    public decimal MinPayoutThreshold { get; set; } = 10m;

    /// <summary>Large-payout threshold (above this requires admin approval). Default 5000.</summary>
    public decimal LargePayoutThreshold { get; set; } = 5000m;

    /// <summary>Escrow hold period in days. Default 7.</summary>
    public int EscrowReleaseDays { get; set; } = 7;
}
