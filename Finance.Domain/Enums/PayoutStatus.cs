namespace Finance.Domain.Enums;

/// <summary>
/// Lifecycle state of a Payout aggregate (F-R7).
/// </summary>
public enum PayoutStatus : byte
{
    /// <summary>Created but not yet approved (or large and awaiting admin approval).</summary>
    Pending = 0,

    /// <summary>Auto-approved (below large threshold) and queued for gateway disbursement.</summary>
    ReadyForPayout = 1,

    /// <summary>Currently held: no verified bank account or open dispute.</summary>
    Hold = 2,

    /// <summary>Successfully transferred to provider's bank.</summary>
    Completed = 3,

    /// <summary>Gateway disbursement failed; retry or manual intervention needed.</summary>
    Failed = 4,

    /// <summary>Reconciled manually by admin outside the gateway flow.</summary>
    ManuallyResolved = 5,
}
