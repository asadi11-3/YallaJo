using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
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
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SubscriptionFeature> SubscriptionFeatures => Set<SubscriptionFeature>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<LoyaltyPoints> LoyaltyPoints => Set<LoyaltyPoints>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<DiscountUsage> DiscountUsages => Set<DiscountUsage>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
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
