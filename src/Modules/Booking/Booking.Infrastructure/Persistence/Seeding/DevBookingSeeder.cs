using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Booking.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-B1 — Development / QA-only seeder that creates basic booking states for manual QA:
/// one <see cref="BookingStatus.AwaitingPayment"/>, one <see cref="BookingStatus.Confirmed"/>, and
/// one <see cref="BookingStatus.Cancelled"/> <see cref="TourBooking"/>, plus the supporting
/// Booking-side <see cref="TourGuide"/> aggregate and an <see cref="AvailabilitySlot"/>.
/// <para>
/// Bookings reference the seeded dev customer (<see cref="DevSeedIds.CustomerUserId"/>) and the dev
/// tour with image (<see cref="DevSeedIds.TourWithImageId"/>). Guarded by
/// <see cref="IHostEnvironment.IsDevelopment"/>; idempotent per-row. Richer financial/dispute/refund
/// states are intentionally deferred to a later patch (B2/B3).
/// </para>
/// </summary>
public sealed class DevBookingSeeder(
    BookingDbContext dbContext,
    IHostEnvironment hostEnvironment,
    ILogger<DevBookingSeeder> logger) : IModuleDbInitializer
{
    public int Order => 166;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var seededAny = false;

        seededAny |= await EnsureTourGuideAsync(cancellationToken);
        seededAny |= await EnsureSlotAsync(cancellationToken);
        seededAny |= await EnsureBookingsAsync(cancellationToken);

        if (seededAny)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("DEV-SEED-B1: seeded development bookings (AwaitingPayment, Confirmed, Cancelled).");
        }
    }

    private async Task<bool> EnsureTourGuideAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.TourGuides
            .IgnoreQueryFilters()
            .AnyAsync(g => g.Id == DevSeedIds.BookingTourGuideId || g.UserId == DevSeedIds.GuideUserId, cancellationToken);

        if (exists)
        {
            return false;
        }

        var guide = CreateEntity<TourGuide>();
        SetProperty(guide, nameof(TourGuide.Id), DevSeedIds.BookingTourGuideId);
        SetProperty(guide, nameof(TourGuide.UserId), DevSeedIds.GuideUserId);
        SetProperty(guide, nameof(TourGuide.Bio), "Dev/QA seeded Booking-side tour guide registry row.");
        SetProperty(guide, nameof(TourGuide.YearsOfExperience), 6);
        SetProperty(guide, nameof(TourGuide.AverageRating), 4.6m);
        SetProperty(guide, nameof(TourGuide.ReviewCount), 8);
        SetProperty(guide, nameof(TourGuide.CompletedTourCount), 12);
        SetProperty(guide, nameof(TourGuide.IsVerified), true);
        SetProperty(guide, nameof(TourGuide.IsActive), true);
        SetProperty(guide, nameof(TourGuide.HourlyRate), 25m);
        SetProperty(guide, nameof(TourGuide.Currency), "JOD");
        SetProperty(guide, nameof(TourGuide.ResponseTimeMinutes), 30);

        dbContext.TourGuides.Add(guide);
        return true;
    }

    private async Task<bool> EnsureSlotAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.AvailabilitySlots
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Id == DevSeedIds.AvailabilitySlotId, cancellationToken);

        if (exists)
        {
            return false;
        }

        var slot = CreateEntity<AvailabilitySlot>();
        SetProperty(slot, nameof(AvailabilitySlot.Id), DevSeedIds.AvailabilitySlotId);
        SetProperty(slot, nameof(AvailabilitySlot.TourGuideId), DevSeedIds.BookingTourGuideId);
        SetProperty(slot, nameof(AvailabilitySlot.SlotType), SlotType.Tour);
        SetProperty(slot, nameof(AvailabilitySlot.TourId), DevSeedIds.TourWithImageId);
        SetProperty(slot, nameof(AvailabilitySlot.Date), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        SetProperty(slot, nameof(AvailabilitySlot.StartTime), new TimeOnly(8, 0));
        SetProperty(slot, nameof(AvailabilitySlot.EndTime), new TimeOnly(16, 0));
        SetProperty(slot, nameof(AvailabilitySlot.MaxCapacity), 12);
        SetProperty(slot, nameof(AvailabilitySlot.BookedCount), 2);
        SetProperty(slot, nameof(AvailabilitySlot.LockedCount), 0);
        SetProperty(slot, nameof(AvailabilitySlot.IsActive), true);

        dbContext.AvailabilitySlots.Add(slot);
        return true;
    }

    private async Task<bool> EnsureBookingsAsync(CancellationToken cancellationToken)
    {
        var existingIds = (await dbContext.TourBookings
                .IgnoreQueryFilters()
                .Where(b => b.Id == DevSeedIds.BookingAwaitingPaymentId
                            || b.Id == DevSeedIds.BookingConfirmedId
                            || b.Id == DevSeedIds.BookingCancelledId)
                .Select(b => b.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var bookings = new List<TourBooking>();

        if (!existingIds.Contains(DevSeedIds.BookingAwaitingPaymentId))
        {
            var awaiting = BuildBooking(
                DevSeedIds.BookingAwaitingPaymentId,
                "YJ-20260701-DEV001",
                participantCount: 1,
                subtotal: 55m,
                BookingStatus.AwaitingPayment);
            SetProperty(awaiting, nameof(TourBooking.PaymentExpiresAt), DateTime.UtcNow.AddMinutes(10));
            bookings.Add(awaiting);
        }

        if (!existingIds.Contains(DevSeedIds.BookingConfirmedId))
        {
            var confirmed = BuildBooking(
                DevSeedIds.BookingConfirmedId,
                "YJ-20260701-DEV002",
                participantCount: 2,
                subtotal: 110m,
                BookingStatus.Confirmed);
            SetProperty(confirmed, nameof(TourBooking.ConfirmedAt), DateTime.UtcNow.AddDays(-2));
            SetProperty(confirmed, nameof(TourBooking.ConfirmationSource), ConfirmationSource.PaymentWebhook);
            bookings.Add(confirmed);
        }

        if (!existingIds.Contains(DevSeedIds.BookingCancelledId))
        {
            var cancelled = BuildBooking(
                DevSeedIds.BookingCancelledId,
                "YJ-20260701-DEV003",
                participantCount: 1,
                subtotal: 55m,
                BookingStatus.Cancelled);
            SetProperty(cancelled, nameof(TourBooking.CancelledAt), DateTime.UtcNow.AddDays(-1));
            SetProperty(cancelled, nameof(TourBooking.CancellationSource), CancellationSource.User);
            SetProperty(cancelled, nameof(TourBooking.CancellationReason), "Customer changed travel plans (dev seed).");
            SetProperty(cancelled, nameof(TourBooking.RefundAmount), (decimal?)55m);
            bookings.Add(cancelled);
        }

        if (bookings.Count == 0)
        {
            return false;
        }

        dbContext.TourBookings.AddRange(bookings);
        return true;
    }

    private static TourBooking BuildBooking(
        Guid id,
        string reference,
        int participantCount,
        decimal subtotal,
        BookingStatus status)
    {
        var commission = decimal.Round(subtotal * 0.10m, 2);

        var booking = CreateEntity<TourBooking>();
        SetProperty(booking, nameof(TourBooking.Id), id);
        SetProperty(booking, nameof(TourBooking.UserId), DevSeedIds.CustomerUserId);
        SetProperty(booking, nameof(TourBooking.TourId), DevSeedIds.TourWithImageId);
        SetProperty(booking, nameof(TourBooking.ProviderId), DevSeedIds.ProviderId);
        SetProperty(booking, nameof(TourBooking.AvailabilitySlotId), DevSeedIds.AvailabilitySlotId);
        SetProperty(booking, nameof(TourBooking.ParticipantCount), participantCount);
        SetProperty(booking, nameof(TourBooking.Reference), reference);
        SetProperty(booking, nameof(TourBooking.Subtotal), subtotal);
        SetProperty(booking, nameof(TourBooking.DiscountAmount), 0m);
        SetProperty(booking, nameof(TourBooking.LoyaltyAmount), 0m);
        SetProperty(booking, nameof(TourBooking.TotalAmount), subtotal);
        SetProperty(booking, nameof(TourBooking.Currency), "JOD");
        SetProperty(booking, nameof(TourBooking.CommissionRate), 0.10m);
        SetProperty(booking, nameof(TourBooking.CommissionAmount), commission);
        SetProperty(booking, nameof(TourBooking.LineItemsJson),
            $"[{{\"TierType\":1,\"Count\":{participantCount},\"UnitPrice\":55.00,\"Currency\":\"JOD\"}}]");
        SetProperty(booking, nameof(TourBooking.RefundPolicySnapshot),
            "{\"FullRefundHours\":72,\"PartialRefundHours\":24,\"PartialRefundPercent\":50}");
        SetProperty(booking, nameof(TourBooking.IsInstantBooking), true);
        SetProperty(booking, nameof(TourBooking.PaymentExpiresAt), DateTime.UtcNow.AddYears(1));
        SetProperty(booking, nameof(TourBooking.Status), status);

        return booking;
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        if (Activator.CreateInstance(typeof(TEntity), nonPublic: true) is not TEntity entity)
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
