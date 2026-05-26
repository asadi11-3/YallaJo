using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Finance.Infrastructure.Persistence;

public sealed class FinanceDbContext : DbContext, IDbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<PayoutItem> PayoutItems => Set<PayoutItem>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<DisputeMessage> DisputeMessages => Set<DisputeMessage>();
    public DbSet<DisputeEvidence> DisputeEvidence => Set<DisputeEvidence>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    // Deferred post-MVP: SubscriptionPlan, SubscriptionFeature, PlanFeature, Subscription, LoyaltyPoints, LoyaltyTransaction, Referral
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<DiscountUsage> DiscountUsages => Set<DiscountUsage>();
    public DbSet<ProviderBankAccount> ProviderBankAccounts => Set<ProviderBankAccount>();
    public DbSet<ProviderPaymentMethod> ProviderPaymentMethods => Set<ProviderPaymentMethod>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceNumberCounter> InvoiceNumberCounters => Set<InvoiceNumberCounter>();
    public DbSet<PaymentExpectation> PaymentExpectations => Set<PaymentExpectation>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("finance");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FinanceDbContext).Assembly,
            type => type.Namespace?.Contains("Finance.Infrastructure.Persistence.Configurations") ?? false
        );

        base.OnModelCreating(modelBuilder);
    }
}
