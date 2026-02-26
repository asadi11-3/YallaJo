using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.Persistence;

public sealed class ContentPlacesDbContext : DbContext, IDbContext
{
    public ContentPlacesDbContext(DbContextOptions<ContentPlacesDbContext> options) : base(options) { }

    public DbSet<Place> Places => Set<Place>();
    public DbSet<PlaceTranslation> PlaceTranslations => Set<PlaceTranslation>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessTranslation> BusinessTranslations => Set<BusinessTranslation>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<PlaceBusiness> PlaceBusinesses => Set<PlaceBusiness>();
    public DbSet<AccessibilityFeature> AccessibilityFeatures => Set<AccessibilityFeature>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content_places");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ContentPlacesDbContext).Assembly,
            type => type.Namespace?.Contains("ContentPlaces.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
