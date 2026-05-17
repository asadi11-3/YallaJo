using YallaJo.SharedKernel.Application.Authorization;

namespace Finance.Contracts.Authorization;

public sealed class FinancePermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Finance";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // Payment (4)
        new(FinanceFeatures.Payment, AppAction.ReadOwn, PermissionGroup.FinanceOperations, "View own payments"),
        new(FinanceFeatures.Payment, AppAction.ReadAny, PermissionGroup.FinanceOperations, "View any payment (admin)"),
        new(FinanceFeatures.Payment, AppAction.Create, PermissionGroup.FinanceOperations, "Initiate a payment"),
        new(FinanceFeatures.Payment, AppAction.Refund, PermissionGroup.FinanceOperations, "Refund a payment"),
        // Payout (3)
        new(FinanceFeatures.Payout, AppAction.ReadOwn, PermissionGroup.FinanceOperations, "View own payouts"),
        new(FinanceFeatures.Payout, AppAction.ReadAny, PermissionGroup.FinanceOperations, "View any payout"),
        new(FinanceFeatures.Payout, AppAction.Trigger, PermissionGroup.FinanceOperations, "Trigger a payout run"),
        // Invoice (3)
        new(FinanceFeatures.Invoice, AppAction.ReadOwn, PermissionGroup.FinanceOperations, "View own invoices"),
        new(FinanceFeatures.Invoice, AppAction.ReadAny, PermissionGroup.FinanceOperations, "View any invoice"),
        new(FinanceFeatures.Invoice, AppAction.Download, PermissionGroup.FinanceOperations, "Download invoice PDF"),
        // Subscription (2)
        new(FinanceFeatures.Subscription, AppAction.ReadOwn, PermissionGroup.FinanceOperations, "View own subscriptions"),
        new(FinanceFeatures.Subscription, AppAction.Cancel, PermissionGroup.FinanceOperations, "Cancel a subscription"),
        // Discount (2)
        new(FinanceFeatures.Discount, AppAction.Create, PermissionGroup.FinanceOperations, "Create a discount code"),
        new(FinanceFeatures.Discount, AppAction.Update, PermissionGroup.FinanceOperations, "Update a discount code"),
        // CommissionRule (2)
        new(FinanceFeatures.CommissionRule, AppAction.Read, PermissionGroup.FinanceOperations, "View commission rules"),
        new(FinanceFeatures.CommissionRule, AppAction.Update, PermissionGroup.FinanceOperations, "Update commission rule"),
        // Dispute (2)
        new(FinanceFeatures.Dispute, AppAction.Read, PermissionGroup.FinanceOperations, "View disputes"),
        new(FinanceFeatures.Dispute, AppAction.Resolve, PermissionGroup.FinanceOperations, "Resolve a dispute"),
        // ProviderBankAccount (1)
        new(FinanceFeatures.ProviderBankAccount, AppAction.UpdateSelf, PermissionGroup.FinanceOperations, "Manage own bank account"),
        // LoyaltyPoints (1)
        new(FinanceFeatures.LoyaltyPoints, AppAction.ReadOwn, PermissionGroup.FinanceOperations, "View own loyalty points"),
        // FinanceAdmin (1)
        new(FinanceFeatures.FinanceAdmin, AppAction.Export, PermissionGroup.FinanceOperations, "Export finance data"),
        // FinanceReports (1)
        new(FinanceFeatures.FinanceReports, AppAction.Read, PermissionGroup.FinanceOperations, "View finance reports"),
    ];
}
