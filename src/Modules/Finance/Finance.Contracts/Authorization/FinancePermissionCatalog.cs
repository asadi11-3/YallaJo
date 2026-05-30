using YallaJo.SharedKernel.Application.Authorization;

namespace Finance.Contracts.Authorization;

/// <summary>
/// Finance module permission surface. Discovered by Security.Infrastructure
/// at boot via <c>IEnumerable&lt;IPermissionCatalog&gt;</c>.
/// </summary>
/// <remarks>
/// Sprint Finance-T0 → 8 features, 27 permissions.
/// Features: Payment(2), Refund(2), Invoice(2), Payout(3), CommissionRule(4),
/// ProviderBankAccount(4), ProviderPaymentMethod(5), AdminFinanceDashboard(5).
/// Group = <c>FinanceOperations</c> for all entries.
/// </remarks>
public sealed class FinancePermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Finance";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── Payment (2) ──────────────────────────────────────────────────────
        new(FinanceFeatures.Payment, AppAction.Create,
            PermissionGroup.FinanceOperations,
            "Initiate a payment for a booking"),
        new(FinanceFeatures.Payment, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View payment details (self / provider / admin)"),

        // ── Refund (2) ───────────────────────────────────────────────────────
        new(FinanceFeatures.Refund, AppAction.Create,
            PermissionGroup.FinanceOperations,
            "Issue a refund against a completed payment"),
        new(FinanceFeatures.Refund, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View refund history"),

        // ── Invoice (2) ──────────────────────────────────────────────────────
        new(FinanceFeatures.Invoice, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View invoice metadata"),
        new(FinanceFeatures.Invoice, AppAction.Download,
            PermissionGroup.FinanceOperations,
            "Download invoice as PDF"),

        // ── Payout (3) ───────────────────────────────────────────────────────
        new(FinanceFeatures.Payout, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View payout records"),
        new(FinanceFeatures.Payout, AppAction.Trigger,
            PermissionGroup.FinanceOperations,
            "Trigger admin payout batch run"),
        new(FinanceFeatures.Payout, AppAction.Approve,
            PermissionGroup.FinanceOperations,
            "Approve a large pending payout"),

        // ── CommissionRule (4) ───────────────────────────────────────────────
        new(FinanceFeatures.CommissionRule, AppAction.Create,
            PermissionGroup.FinanceOperations,
            "Create a commission rule"),
        new(FinanceFeatures.CommissionRule, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View commission rules"),
        new(FinanceFeatures.CommissionRule, AppAction.Update,
            PermissionGroup.FinanceOperations,
            "Update a commission rule"),
        new(FinanceFeatures.CommissionRule, AppAction.Delete,
            PermissionGroup.FinanceOperations,
            "Soft-delete a commission rule"),

        // ── ProviderBankAccount (4) ──────────────────────────────────────────
        new(FinanceFeatures.ProviderBankAccount, AppAction.Create,
            PermissionGroup.FinanceOperations,
            "Register a provider bank account"),
        new(FinanceFeatures.ProviderBankAccount, AppAction.Update,
            PermissionGroup.FinanceOperations,
            "Update provider bank account details"),
        new(FinanceFeatures.ProviderBankAccount, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View provider bank account"),
        new(FinanceFeatures.ProviderBankAccount, AppAction.Verify,
            PermissionGroup.FinanceOperations,
            "Verify a provider bank account (admin/KYC)"),

        // ── ProviderPaymentMethod (5) ────────────────────────────────────────
        new(FinanceFeatures.ProviderPaymentMethod, AppAction.Create,
            PermissionGroup.FinanceOperations,
            "Register a provider payment method"),
        new(FinanceFeatures.ProviderPaymentMethod, AppAction.Update,
            PermissionGroup.FinanceOperations,
            "Update provider payment method details"),
        new(FinanceFeatures.ProviderPaymentMethod, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View provider payment methods"),
        new(FinanceFeatures.ProviderPaymentMethod, AppAction.Delete,
            PermissionGroup.FinanceOperations,
            "Delete provider payment method"),
        new(FinanceFeatures.ProviderPaymentMethod, AppAction.Verify,
            PermissionGroup.FinanceOperations,
            "Verify a provider payment method (admin/KYC)"),

        // ── AdminFinanceDashboard (3) ────────────────────────────────────────
        new(FinanceFeatures.AdminFinanceDashboard, AppAction.Read,
            PermissionGroup.FinanceOperations,
            "View admin finance dashboard"),
        new(FinanceFeatures.AdminFinanceDashboard, AppAction.Export,
            PermissionGroup.FinanceOperations,
            "Export finance reports (CSV/Excel)"),
        new(FinanceFeatures.AdminFinanceDashboard, AppAction.Refresh,
            PermissionGroup.FinanceOperations,
            "Refresh admin finance dashboard caches"),
        new(FinanceFeatures.AdminFinanceDashboard, AppAction.Update,
            PermissionGroup.FinanceOperations,
            "Update admin finance workflow items"),
        new(FinanceFeatures.AdminFinanceDashboard, AppAction.Approve,
            PermissionGroup.FinanceOperations,
            "Approve or resolve admin finance workflow items"),
    ];
}
