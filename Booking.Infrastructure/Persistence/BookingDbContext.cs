using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
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
    public DbSet<PackageBooking> PackageBookings => Set<PackageBooking>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();
    public DbSet<SlotLock> SlotLocks => Set<SlotLock>();
    public DbSet<RefundPolicy> RefundPolicies => Set<RefundPolicy>();
    public DbSet<CommissionSnapshot> CommissionSnapshots => Set<CommissionSnapshot>();
    public DbSet<ProviderDocument> ProviderDocuments => Set<ProviderDocument>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BookingDbContext).Assembly,
            type => type.Namespace?.Contains("Booking.Infrastructure.Persistence.Configurations") ?? false
        );

        var providerName = Database.ProviderName;
        if (!string.IsNullOrEmpty(providerName)
            && providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var columnType = property.GetColumnType();
                    if (!string.IsNullOrEmpty(columnType)
                        && columnType.StartsWith("nvarchar(max)", StringComparison.OrdinalIgnoreCase))
                    {
                        property.SetColumnType("TEXT");
                    }

                    if (property.IsConcurrencyToken
                        && property.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate
                        && property.ClrType == typeof(byte[]))
                    {
                        property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                    }
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
