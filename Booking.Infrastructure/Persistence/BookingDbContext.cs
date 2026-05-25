using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Booking.Infrastructure.Persistence;

public sealed class BookingDbContext : DbContext, IDbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<TourGuide> TourGuides => Set<TourGuide>();
    public DbSet<TourGuideLanguage> TourGuideLanguages => Set<TourGuideLanguage>();
    public DbSet<TourGuideSpecialization> TourGuideSpecializations => Set<TourGuideSpecialization>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();
    public DbSet<TourBooking> TourBookings => Set<TourBooking>();
    // Deferred post-MVP: PackageBooking, Reservation (business reservation future)
    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();
    public DbSet<SlotLock> SlotLocks => Set<SlotLock>();
    public DbSet<RefundPolicy> RefundPolicies => Set<RefundPolicy>();
    public DbSet<ProviderDocument> ProviderDocuments => Set<ProviderDocument>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BookingDbContext).Assembly,
            type => type.Namespace?.Contains("Booking.Infrastructure.Persistence.Configurations") ?? false
        );

        base.OnModelCreating(modelBuilder);
    }
}
