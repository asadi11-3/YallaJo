using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.Persistence;

public sealed class ContentToursDbContext : DbContext, IDbContext
{
    public ContentToursDbContext(DbContextOptions<ContentToursDbContext> options) : base(options) { }

    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourTranslation> TourTranslations => Set<TourTranslation>();
    public DbSet<TourSchedule> TourSchedules => Set<TourSchedule>();
    public DbSet<TourWaypoint> TourWaypoints => Set<TourWaypoint>();
    public DbSet<TourPricingTier> TourPricingTiers => Set<TourPricingTier>();
    public DbSet<TourPricingTierTranslation> TourPricingTierTranslations => Set<TourPricingTierTranslation>();
    public DbSet<TourGuide> TourGuides => Set<TourGuide>();
    public DbSet<TourGuideLanguage> TourGuideLanguages => Set<TourGuideLanguage>();
    public DbSet<TourGuideSpecialization> TourGuideSpecializations => Set<TourGuideSpecialization>();
    public DbSet<TourTourGuide> TourTourGuides => Set<TourTourGuide>();
    public DbSet<TourPackage> TourPackages => Set<TourPackage>();
    public DbSet<TourPackageTour> TourPackageTours => Set<TourPackageTour>();
    public DbSet<TourPackageInclusion> TourPackageInclusions => Set<TourPackageInclusion>();
    public DbSet<TourChildFacility> TourChildFacilities => Set<TourChildFacility>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content_tours");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ContentToursDbContext).Assembly,
            type => type.Namespace?.Contains("ContentTours.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
