using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Finance.Infrastructure.Persistence.Seeding;

public sealed class FinanceDbInitializer(FinanceDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid TravelerOne = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid GuideOne = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PlanId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000001");
    private static readonly Guid SubscriptionId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000002");
    private static readonly Guid PaymentOneId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000003");
    private static readonly Guid PaymentTwoId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000004");
    private static readonly Guid PayoutId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000005");
    private static readonly Guid DiscountId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000006");
    private static readonly Guid LoyaltyId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000007");

    public int Order => 100;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Payments.AnyAsync(cancellationToken))
        {
            return;
        }

        var plan = CreateSubscriptionPlan();
        var subscription = CreateSubscription();
        var payments = CreatePayments();
        var payout = CreatePayout();
        var payoutItems = CreatePayoutItems();
        var loyaltyPoints = CreateLoyaltyPoints();
        var loyaltyTransactions = CreateLoyaltyTransactions();
        var discount = CreateDiscount();
        var discountUsage = CreateDiscountUsage();
        var referral = CreateReferral();

        dbContext.SubscriptionPlans.Add(plan);
        dbContext.Subscriptions.Add(subscription);
        dbContext.Payments.AddRange(payments);
        dbContext.Payouts.Add(payout);
        dbContext.PayoutItems.AddRange(payoutItems);
        dbContext.LoyaltyPoints.Add(loyaltyPoints);
        dbContext.LoyaltyTransactions.AddRange(loyaltyTransactions);
        dbContext.Discounts.Add(discount);
        dbContext.DiscountUsages.Add(discountUsage);
        dbContext.Referrals.Add(referral);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SubscriptionPlan CreateSubscriptionPlan()
    {
        var plan = CreateEntity<SubscriptionPlan>();
        SetProperty(plan, nameof(SubscriptionPlan.Id), PlanId);
        SetProperty(plan, nameof(SubscriptionPlan.Name), "Guide Pro");
        SetProperty(plan, nameof(SubscriptionPlan.Description), "Premium tools and boosted listing visibility.");
        SetProperty(plan, nameof(SubscriptionPlan.Price), new Money(29m, "JOD"));
        SetProperty(plan, nameof(SubscriptionPlan.Currency), "JOD");
        SetProperty(plan, nameof(SubscriptionPlan.BillingCycle), BillingCycle.Monthly);
        SetProperty(plan, nameof(SubscriptionPlan.TrialDays), 7);
        SetProperty(plan, nameof(SubscriptionPlan.IsActive), true);
        SetProperty(plan, nameof(SubscriptionPlan.SortOrder), 1);
        return plan;
    }

    private static Subscription CreateSubscription()
    {
        var subscription = CreateEntity<Subscription>();
        SetProperty(subscription, nameof(Subscription.Id), SubscriptionId);
        SetProperty(subscription, nameof(Subscription.UserId), GuideOne);
        SetProperty(subscription, nameof(Subscription.PlanId), PlanId);
        SetProperty(subscription, nameof(Subscription.Status), SubscriptionStatus.Active);
        SetProperty(subscription, nameof(Subscription.ActivePeriod), new DateRange(DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddMonths(1)));
        SetProperty(subscription, nameof(Subscription.TrialEndsAt), DateTime.UtcNow.AddDays(4));
        return subscription;
    }

    private static List<Payment> CreatePayments()
    {
        var paymentOne = CreateEntity<Payment>();
        SetProperty(paymentOne, nameof(Payment.Id), PaymentOneId);
        SetProperty(paymentOne, nameof(Payment.UserId), TravelerOne);
        SetProperty(paymentOne, nameof(Payment.BookingId), SeedBookingIds.BookingOne);
        SetProperty(paymentOne, nameof(Payment.Amount), new Money(150m, "JOD"));
        SetProperty(paymentOne, nameof(Payment.Currency), "JOD");
        SetProperty(paymentOne, nameof(Payment.PaymentMethod), PaymentMethod.CreditCard);
        SetProperty(paymentOne, nameof(Payment.Status), PaymentStatus.Completed);
        SetProperty(paymentOne, nameof(Payment.TransactionId), "txn-booking-001");
        SetProperty(paymentOne, nameof(Payment.PaidAt), DateTime.UtcNow.AddDays(-1));
        SetProperty(paymentOne, nameof(Payment.RefundedAmount), Money.Zero("JOD"));

        var paymentTwo = CreateEntity<Payment>();
        SetProperty(paymentTwo, nameof(Payment.Id), PaymentTwoId);
        SetProperty(paymentTwo, nameof(Payment.UserId), TravelerTwo);
        SetProperty(paymentTwo, nameof(Payment.ReservationId), SeedBookingIds.ReservationOne);
        SetProperty(paymentTwo, nameof(Payment.Amount), new Money(60m, "JOD"));
        SetProperty(paymentTwo, nameof(Payment.Currency), "JOD");
        SetProperty(paymentTwo, nameof(Payment.PaymentMethod), PaymentMethod.DebitCard);
        SetProperty(paymentTwo, nameof(Payment.Status), PaymentStatus.Completed);
        SetProperty(paymentTwo, nameof(Payment.TransactionId), "txn-reservation-001");
        SetProperty(paymentTwo, nameof(Payment.PaidAt), DateTime.UtcNow.AddHours(-20));
        SetProperty(paymentTwo, nameof(Payment.RefundedAmount), Money.Zero("JOD"));

        return [paymentOne, paymentTwo];
    }

    private static Payout CreatePayout()
    {
        var payout = CreateEntity<Payout>();
        SetProperty(payout, nameof(Payout.Id), PayoutId);
        SetProperty(payout, nameof(Payout.RecipientUserId), GuideOne);
        SetProperty(payout, nameof(Payout.Status), PayoutStatus.Processing);
        SetProperty(payout, nameof(Payout.TotalAmount), new Money(120m, "JOD"));
        SetProperty(payout, nameof(Payout.Currency), "JOD");
        SetProperty(payout, nameof(Payout.BankAccountInfo), "Jordan Bank - ****3481");
        return payout;
    }

    private static List<PayoutItem> CreatePayoutItems()
    {
        var item = CreateEntity<PayoutItem>();
        SetProperty(item, nameof(PayoutItem.PayoutId), PayoutId);
        SetProperty(item, nameof(PayoutItem.BookingId), SeedBookingIds.BookingOne);
        SetProperty(item, nameof(PayoutItem.Amount), new Money(150m, "JOD"));
        SetProperty(item, nameof(PayoutItem.Commission), new Money(30m, "JOD"));
        SetProperty(item, nameof(PayoutItem.NetAmount), new Money(120m, "JOD"));
        return [item];
    }

    private static LoyaltyPoints CreateLoyaltyPoints()
    {
        var points = CreateEntity<LoyaltyPoints>();
        SetProperty(points, nameof(LoyaltyPoints.Id), LoyaltyId);
        SetProperty(points, nameof(LoyaltyPoints.UserId), TravelerOne);
        SetProperty(points, nameof(LoyaltyPoints.TotalPoints), 150);
        SetProperty(points, nameof(LoyaltyPoints.AvailablePoints), 100);
        SetProperty(points, nameof(LoyaltyPoints.LifetimePoints), 150);
        return points;
    }

    private static List<LoyaltyTransaction> CreateLoyaltyTransactions()
    {
        var earned = CreateEntity<LoyaltyTransaction>();
        SetProperty(earned, nameof(LoyaltyTransaction.LoyaltyPointsId), LoyaltyId);
        SetProperty(earned, nameof(LoyaltyTransaction.Points), 150);
        SetProperty(earned, nameof(LoyaltyTransaction.TransactionType), TransactionType.Earned);
        SetProperty(earned, nameof(LoyaltyTransaction.Description), "Earned from completed booking.");
        SetProperty(earned, nameof(LoyaltyTransaction.ReferenceId), SeedBookingIds.BookingOne);

        var redeemed = CreateEntity<LoyaltyTransaction>();
        SetProperty(redeemed, nameof(LoyaltyTransaction.LoyaltyPointsId), LoyaltyId);
        SetProperty(redeemed, nameof(LoyaltyTransaction.Points), -50);
        SetProperty(redeemed, nameof(LoyaltyTransaction.TransactionType), TransactionType.Redeemed);
        SetProperty(redeemed, nameof(LoyaltyTransaction.Description), "Redeemed at checkout.");
        SetProperty(redeemed, nameof(LoyaltyTransaction.ReferenceId), SeedBookingIds.BookingTwo);

        return [earned, redeemed];
    }

    private static Discount CreateDiscount()
    {
        var discount = CreateEntity<Discount>();
        SetProperty(discount, nameof(Discount.Id), DiscountId);
        SetProperty(discount, nameof(Discount.Code), "WELCOME10");
        SetProperty(discount, nameof(Discount.Name), "Welcome 10%");
        SetProperty(discount, nameof(Discount.Description), "Welcome discount for first booking.");
        SetProperty(discount, nameof(Discount.DiscountType), DiscountType.Percentage);
        SetProperty(discount, nameof(Discount.DiscountValue), new Money(10m, "JOD"));
        SetProperty(discount, nameof(Discount.MinOrderAmount), new Money(30m, "JOD"));
        SetProperty(discount, nameof(Discount.MaxDiscountAmount), new Money(25m, "JOD"));
        SetProperty(discount, nameof(Discount.MaxUsageCount), 1000);
        SetProperty(discount, nameof(Discount.CurrentUsageCount), 1);
        SetProperty(discount, nameof(Discount.ValidityPeriod), new DateRange(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddMonths(3)));
        SetProperty(discount, nameof(Discount.IsActive), true);
        SetProperty(discount, nameof(Discount.TargetScope), DiscountTargetScope.AllMyTours);
        SetProperty(discount, nameof(Discount.Visibility), DiscountVisibility.PromoCode);
        return discount;
    }

    private static DiscountUsage CreateDiscountUsage()
    {
        var usage = CreateEntity<DiscountUsage>();
        SetProperty(usage, nameof(DiscountUsage.DiscountId), DiscountId);
        SetProperty(usage, nameof(DiscountUsage.UserId), TravelerOne);
        SetProperty(usage, nameof(DiscountUsage.BookingId), SeedBookingIds.BookingOne);
        SetProperty(usage, nameof(DiscountUsage.Amount), new Money(15m, "JOD"));
        SetProperty(usage, nameof(DiscountUsage.UsedAt), DateTime.UtcNow.AddDays(-1));
        return usage;
    }

    private static Referral CreateReferral()
    {
        var referral = CreateEntity<Referral>();
        SetProperty(referral, nameof(Referral.ReferrerUserId), TravelerOne);
        SetProperty(referral, nameof(Referral.ReferredUserId), TravelerTwo);
        SetProperty(referral, nameof(Referral.ReferralCode), "REF-NOOR-2026");
        SetProperty(referral, nameof(Referral.Status), (byte)1);
        SetProperty(referral, nameof(Referral.RewardAmount), 5m);
        SetProperty(referral, nameof(Referral.ReferredRewardAmount), 3m);
        SetProperty(referral, nameof(Referral.Currency), "JOD");
        SetProperty(referral, nameof(Referral.CompletedAt), DateTime.UtcNow.AddDays(-2));
        return referral;
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
        {
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        }

        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
