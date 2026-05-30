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
    private static readonly Guid PaymentOneId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000003");
    private static readonly Guid PaymentTwoId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000004");
    private static readonly Guid PayoutId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000005");
    private static readonly Guid DiscountId = Guid.Parse("f1f1f1f1-0000-0000-0000-000000000006");
    // LoyaltyId removed — LoyaltyPoints deferred post-MVP

    public int Order => 100;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Payments.AnyAsync(cancellationToken))
        {
            return;
        }

        var payments = CreatePayments();
        var payout = CreatePayout();
        var payoutItems = CreatePayoutItems();
        var discount = CreateDiscount();
        var discountUsage = CreateDiscountUsage();

        // Deferred post-MVP: SubscriptionPlan, Subscription, LoyaltyPoints, LoyaltyTransaction, Referral removed
        dbContext.Payments.AddRange(payments);
        dbContext.Payouts.Add(payout);
        dbContext.PayoutItems.AddRange(payoutItems);
        dbContext.Discounts.Add(discount);
        dbContext.DiscountUsages.Add(discountUsage);

        await dbContext.SaveChangesAsync(cancellationToken);
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
        SetProperty(paymentTwo, nameof(Payment.BookingId), SeedBookingIds.BookingTwo);
        SetProperty(paymentTwo, nameof(Payment.Amount), new Money(60m, "JOD"));
        SetProperty(paymentTwo, nameof(Payment.Currency), "JOD");
        SetProperty(paymentTwo, nameof(Payment.PaymentMethod), PaymentMethod.DebitCard);
        SetProperty(paymentTwo, nameof(Payment.Status), PaymentStatus.Completed);
        SetProperty(paymentTwo, nameof(Payment.TransactionId), "txn-booking-002");
        SetProperty(paymentTwo, nameof(Payment.PaidAt), DateTime.UtcNow.AddHours(-20));
        SetProperty(paymentTwo, nameof(Payment.RefundedAmount), Money.Zero("JOD"));

        return [paymentOne, paymentTwo];
    }

    private static Payout CreatePayout()
    {
        var payout = CreateEntity<Payout>();
        SetProperty(payout, nameof(Payout.Id), PayoutId);
        SetProperty(payout, nameof(Payout.RecipientUserId), GuideOne);
        SetProperty(payout, nameof(Payout.ProviderId), GuideOne);
        SetProperty(payout, nameof(Payout.Status), PayoutStatus.ReadyForPayout);
        SetProperty(payout, nameof(Payout.TotalAmount), new Money(120m, "JOD"));
        SetProperty(payout, nameof(Payout.GrossAmount), new Money(150m, "JOD"));
        SetProperty(payout, nameof(Payout.CommissionAmount), new Money(30m, "JOD"));
        SetProperty(payout, nameof(Payout.NetAmount), new Money(120m, "JOD"));
        SetProperty(payout, nameof(Payout.Currency), "JOD");
        SetProperty(payout, nameof(Payout.BatchPeriodStart), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)));
        SetProperty(payout, nameof(Payout.BatchPeriodEnd), DateOnly.FromDateTime(DateTime.UtcNow));
        SetProperty(payout, nameof(Payout.BankAccountInfo), "Jordan Bank - ****3481");
        return payout;
    }

    private static List<PayoutItem> CreatePayoutItems()
    {
        var item = CreateEntity<PayoutItem>();
        SetProperty(item, nameof(PayoutItem.PayoutId), PayoutId);
        SetProperty(item, nameof(PayoutItem.BookingId), SeedBookingIds.BookingOne);
        SetProperty(item, nameof(PayoutItem.GrossAmount), new Money(150m, "JOD"));
        SetProperty(item, nameof(PayoutItem.CommissionAmount), new Money(30m, "JOD"));
        SetProperty(item, nameof(PayoutItem.NetAmount), new Money(120m, "JOD"));
        return [item];
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
