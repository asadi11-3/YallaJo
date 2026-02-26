using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
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
    public DbSet<TourTourGuide> TourTourGuides => Set<TourTourGuide>();
    public DbSet<TourPackage> TourPackages => Set<TourPackage>();
    public DbSet<TourPackageInclusion> TourPackageInclusions => Set<TourPackageInclusion>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content_tours");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ContentToursDbContext).Assembly,
            type => type.Namespace?.Contains("ContentTours.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
