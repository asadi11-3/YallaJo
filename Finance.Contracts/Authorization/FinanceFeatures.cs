namespace Finance.Contracts.Authorization;

/// <summary>
/// Finance module feature surface. Each constant maps to a permission feature
/// recognised by <see cref="FinancePermissionCatalog"/>.
/// </summary>
public static class FinanceFeatures
{
    /// <summary>Customer payments (booking-bound, escrow-routed).</summary>
    public const string Payment               = nameof(Payment);

    /// <summary>Refunds against completed payments (separate permission surface).</summary>
    public const string Refund                = nameof(Refund);

    /// <summary>Invoice records auto-generated on payment completion.</summary>
    public const string Invoice               = nameof(Invoice);

    /// <summary>Provider payouts (escrow release + commission deduction).</summary>
    public const string Payout                = nameof(Payout);

    /// <summary>Platform commission tiering rules.</summary>
    public const string CommissionRule        = nameof(CommissionRule);

    /// <summary>Provider bank account records (KYC + payout destination).</summary>
    public const string ProviderBankAccount   = nameof(ProviderBankAccount);

    /// <summary>Admin finance dashboard: aggregated reports + exports.</summary>
    public const string AdminFinanceDashboard = nameof(AdminFinanceDashboard);
}
