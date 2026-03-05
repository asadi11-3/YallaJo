using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Booking.Infrastructure.Persistence.Seeding;

public sealed class BookingDbInitializer(BookingDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid GuideOne = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid GuideTwo = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TravelerOne = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid SlotOneId = Guid.Parse("abababab-1111-1111-1111-111111111111");
    private static readonly Guid SlotTwoId = Guid.Parse("abababab-2222-2222-2222-222222222222");
    private static readonly Guid BookingTourGuideOneId = Guid.Parse("abababab-3333-3333-3333-333333333333");
    private static readonly Guid BookingTourGuideTwoId = Guid.Parse("abababab-4444-4444-4444-444444444444");

    public int Order => 90;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.TourGuides.AnyAsync(cancellationToken))
        {
            return;
        }

        var tourGuides = CreateTourGuides();
        var guideLanguages = CreateGuideLanguages();
        var guideSpecializations = CreateGuideSpecializations();
        var slots = CreateAvailabilitySlots();
        var bookings = CreateTourBookings();
        var reservations = CreateReservations();
        var packageBookings = CreatePackageBookings();

        dbContext.TourGuides.AddRange(tourGuides);
        dbContext.TourGuideLanguages.AddRange(guideLanguages);
        dbContext.TourGuideSpecializations.AddRange(guideSpecializations);
        dbContext.AvailabilitySlots.AddRange(slots);
        dbContext.TourBookings.AddRange(bookings);
        dbContext.Reservations.AddRange(reservations);
        dbContext.PackageBookings.AddRange(packageBookings);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<TourGuide> CreateTourGuides()
    {
        var first = CreateEntity<TourGuide>();
        SetProperty(first, nameof(TourGuide.Id), BookingTourGuideOneId);
        SetProperty(first, nameof(TourGuide.UserId), GuideOne);
        SetProperty(first, nameof(TourGuide.Bio), "Licensed Petra guide with 7 years experience.");
        SetProperty(first, nameof(TourGuide.YearsOfExperience), 7);
        SetProperty(first, nameof(TourGuide.AverageRating), 4.7m);
        SetProperty(first, nameof(TourGuide.ReviewCount), 220);
        SetProperty(first, nameof(TourGuide.CompletedTourCount), 480);
        SetProperty(first, nameof(TourGuide.IsVerified), true);
        SetProperty(first, nameof(TourGuide.IsActive), true);
        SetProperty(first, nameof(TourGuide.HourlyRate), 25m);
        SetProperty(first, nameof(TourGuide.Currency), "JOD");
        SetProperty(first, nameof(TourGuide.ResponseTimeMinutes), 30);

        var second = CreateEntity<TourGuide>();
        SetProperty(second, nameof(TourGuide.Id), BookingTourGuideTwoId);
        SetProperty(second, nameof(TourGuide.UserId), GuideTwo);
        SetProperty(second, nameof(TourGuide.Bio), "Outdoor and canyoning specialist.");
        SetProperty(second, nameof(TourGuide.YearsOfExperience), 5);
        SetProperty(second, nameof(TourGuide.AverageRating), 4.6m);
        SetProperty(second, nameof(TourGuide.ReviewCount), 140);
        SetProperty(second, nameof(TourGuide.CompletedTourCount), 300);
        SetProperty(second, nameof(TourGuide.IsVerified), true);
        SetProperty(second, nameof(TourGuide.IsActive), true);
        SetProperty(second, nameof(TourGuide.HourlyRate), 22m);
        SetProperty(second, nameof(TourGuide.Currency), "JOD");
        SetProperty(second, nameof(TourGuide.ResponseTimeMinutes), 45);

        return [first, second];
    }

    private static List<TourGuideLanguage> CreateGuideLanguages()
    {
        var language1 = CreateEntity<TourGuideLanguage>();
        SetProperty(language1, nameof(TourGuideLanguage.TourGuideId), BookingTourGuideOneId);
        SetProperty(language1, nameof(TourGuideLanguage.LanguageId), Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"));
        SetProperty(language1, nameof(TourGuideLanguage.ProficiencyLevel), (byte)5);

        var language2 = CreateEntity<TourGuideLanguage>();
        SetProperty(language2, nameof(TourGuideLanguage.TourGuideId), BookingTourGuideTwoId);
        SetProperty(language2, nameof(TourGuideLanguage.LanguageId), Guid.Parse("eeeeeeee-0000-0000-0000-000000000002"));
        SetProperty(language2, nameof(TourGuideLanguage.ProficiencyLevel), (byte)4);

        return [language1, language2];
    }

    private static List<TourGuideSpecialization> CreateGuideSpecializations()
    {
        var specialization1 = CreateEntity<TourGuideSpecialization>();
        SetProperty(specialization1, nameof(TourGuideSpecialization.TourGuideId), BookingTourGuideOneId);
        SetProperty(specialization1, nameof(TourGuideSpecialization.SpecializationId), Guid.Parse("12121212-1212-1212-1212-121212121212"));

        var specialization2 = CreateEntity<TourGuideSpecialization>();
        SetProperty(specialization2, nameof(TourGuideSpecialization.TourGuideId), BookingTourGuideTwoId);
        SetProperty(specialization2, nameof(TourGuideSpecialization.SpecializationId), Guid.Parse("34343434-3434-3434-3434-343434343434"));

        return [specialization1, specialization2];
    }

    private static List<AvailabilitySlot> CreateAvailabilitySlots()
    {
        var slotOne = CreateEntity<AvailabilitySlot>();
        SetProperty(slotOne, nameof(AvailabilitySlot.Id), SlotOneId);
        SetProperty(slotOne, nameof(AvailabilitySlot.TourGuideId), BookingTourGuideOneId);
        SetProperty(slotOne, nameof(AvailabilitySlot.SlotType), SlotType.Tour);
        SetProperty(slotOne, nameof(AvailabilitySlot.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(slotOne, nameof(AvailabilitySlot.Date), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        SetProperty(slotOne, nameof(AvailabilitySlot.StartTime), new TimeOnly(8, 0));
        SetProperty(slotOne, nameof(AvailabilitySlot.EndTime), new TimeOnly(16, 0));
        SetProperty(slotOne, nameof(AvailabilitySlot.MaxCapacity), 18);
        SetProperty(slotOne, nameof(AvailabilitySlot.BookedCount), 2);
        SetProperty(slotOne, nameof(AvailabilitySlot.LockedCount), 0);
        SetProperty(slotOne, nameof(AvailabilitySlot.IsActive), true);

        var slotTwo = CreateEntity<AvailabilitySlot>();
        SetProperty(slotTwo, nameof(AvailabilitySlot.Id), SlotTwoId);
        SetProperty(slotTwo, nameof(AvailabilitySlot.TourGuideId), BookingTourGuideTwoId);
        SetProperty(slotTwo, nameof(AvailabilitySlot.SlotType), SlotType.Business);
        SetProperty(slotTwo, nameof(AvailabilitySlot.BusinessId), SeedContentIds.BusinessPetraGuides);
        SetProperty(slotTwo, nameof(AvailabilitySlot.Date), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));
        SetProperty(slotTwo, nameof(AvailabilitySlot.StartTime), new TimeOnly(18, 0));
        SetProperty(slotTwo, nameof(AvailabilitySlot.EndTime), new TimeOnly(20, 0));
        SetProperty(slotTwo, nameof(AvailabilitySlot.MaxCapacity), 8);
        SetProperty(slotTwo, nameof(AvailabilitySlot.BookedCount), 1);
        SetProperty(slotTwo, nameof(AvailabilitySlot.LockedCount), 0);
        SetProperty(slotTwo, nameof(AvailabilitySlot.IsActive), true);

        return [slotOne, slotTwo];
    }

    private static List<TourBooking> CreateTourBookings()
    {
        var first = CreateEntity<TourBooking>();
        SetProperty(first, nameof(TourBooking.Id), SeedBookingIds.BookingOne);
        SetProperty(first, nameof(TourBooking.UserId), TravelerOne);
        SetProperty(first, nameof(TourBooking.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(first, nameof(TourBooking.TourGuideId), BookingTourGuideOneId);
        SetProperty(first, nameof(TourBooking.AvailabilitySlotId), SlotOneId);
        SetProperty(first, nameof(TourBooking.ScheduledDate), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
        SetProperty(first, nameof(TourBooking.StartTime), new TimeOnly(8, 0));
        SetProperty(first, nameof(TourBooking.ParticipantCount), 2);
        SetProperty(first, nameof(TourBooking.TotalPrice), new Money(150m, "JOD"));
        SetProperty(first, nameof(TourBooking.Currency), "JOD");
        SetProperty(first, nameof(TourBooking.ConfirmationCode), "YJ-BOOK-001");
        SetProperty(first, nameof(TourBooking.Status), BookingStatus.Confirmed);
        SetProperty(first, nameof(TourBooking.ConfirmedAt), DateTime.UtcNow);

        var second = CreateEntity<TourBooking>();
        SetProperty(second, nameof(TourBooking.Id), SeedBookingIds.BookingTwo);
        SetProperty(second, nameof(TourBooking.UserId), TravelerTwo);
        SetProperty(second, nameof(TourBooking.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(second, nameof(TourBooking.TourGuideId), BookingTourGuideOneId);
        SetProperty(second, nameof(TourBooking.AvailabilitySlotId), SlotOneId);
        SetProperty(second, nameof(TourBooking.ScheduledDate), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)));
        SetProperty(second, nameof(TourBooking.StartTime), new TimeOnly(8, 0));
        SetProperty(second, nameof(TourBooking.ParticipantCount), 1);
        SetProperty(second, nameof(TourBooking.TotalPrice), new Money(75m, "JOD"));
        SetProperty(second, nameof(TourBooking.Currency), "JOD");
        SetProperty(second, nameof(TourBooking.ConfirmationCode), "YJ-BOOK-002");
        SetProperty(second, nameof(TourBooking.Status), BookingStatus.Pending);

        return [first, second];
    }

    private static List<Reservation> CreateReservations()
    {
        var reservation = CreateEntity<Reservation>();
        SetProperty(reservation, nameof(Reservation.Id), SeedBookingIds.ReservationOne);
        SetProperty(reservation, nameof(Reservation.UserId), TravelerTwo);
        SetProperty(reservation, nameof(Reservation.BusinessId), SeedContentIds.BusinessPetraGuides);
        SetProperty(reservation, nameof(Reservation.AvailabilitySlotId), SlotTwoId);
        SetProperty(reservation, nameof(Reservation.ReservationDate), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));
        SetProperty(reservation, nameof(Reservation.ReservationTime), new TimeOnly(18, 0));
        SetProperty(reservation, nameof(Reservation.PartySize), 3);
        SetProperty(reservation, nameof(Reservation.TotalPrice), 60m);
        SetProperty(reservation, nameof(Reservation.TotalPriceCurrency), "JOD");
        SetProperty(reservation, nameof(Reservation.Currency), "JOD");
        SetProperty(reservation, nameof(Reservation.Status), BookingStatus.Confirmed);
        return [reservation];
    }

    private static List<PackageBooking> CreatePackageBookings()
    {
        var packageBooking = CreateEntity<PackageBooking>();
        SetProperty(packageBooking, nameof(PackageBooking.UserId), TravelerOne);
        SetProperty(packageBooking, nameof(PackageBooking.TourPackageId), SeedContentIds.TourPackageEssentials);
        SetProperty(packageBooking, nameof(PackageBooking.BookingDate), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));
        SetProperty(packageBooking, nameof(PackageBooking.ParticipantCount), 2);
        SetProperty(packageBooking, nameof(PackageBooking.TotalPrice), new Money(190m, "JOD"));
        SetProperty(packageBooking, nameof(PackageBooking.Currency), "JOD");
        SetProperty(packageBooking, nameof(PackageBooking.Status), BookingStatus.Pending);
        return [packageBooking];
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
