using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Analytics.Infrastructure.Persistence;

public sealed class AnalyticsDbContext : DbContext, IDbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserInteraction> UserInteractions => Set<UserInteraction>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<UserPreferredCategory> UserPreferredCategories => Set<UserPreferredCategory>();
    public DbSet<PopularityScore> PopularityScores => Set<PopularityScore>();
    public DbSet<EntityPopularitySnapshot> EntityPopularitySnapshots => Set<EntityPopularitySnapshot>();
    public DbSet<DashboardCache> DashboardCaches => Set<DashboardCache>();
    public DbSet<IngestDebounceMarker> IngestDebounceMarkers => Set<IngestDebounceMarker>();
    public DbSet<PaymentSnapshot> PaymentSnapshots => Set<PaymentSnapshot>();
    public DbSet<BookingSnapshot> BookingSnapshots => Set<BookingSnapshot>();
    public DbSet<RecommendationCache> RecommendationCaches => Set<RecommendationCache>();
    public DbSet<EntityAttributeSnapshot> EntityAttributeSnapshots => Set<EntityAttributeSnapshot>();
    public DbSet<SuggestionBatch> SuggestionBatches => Set<SuggestionBatch>();
    public DbSet<BoostPackage> BoostPackages => Set<BoostPackage>();
    public DbSet<EditorialPin> EditorialPins => Set<EditorialPin>();
    public DbSet<UserExcludedEntity> UserExcludedEntities => Set<UserExcludedEntity>();
    public DbSet<SeasonalityRule> SeasonalityRules => Set<SeasonalityRule>();
    public DbSet<HolidayCalendar> HolidayCalendars => Set<HolidayCalendar>();
    public DbSet<TripArc> TripArcs => Set<TripArc>();
    public DbSet<SponsoredClickEvent> SponsoredClickEvents => Set<SponsoredClickEvent>();
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<ExperimentAssignment> ExperimentAssignments => Set<ExperimentAssignment>();
    public DbSet<SuggestionMetric> SuggestionMetrics => Set<SuggestionMetric>();
    public DbSet<GdprDeletionRequest> GdprDeletionRequests => Set<GdprDeletionRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("analytics");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AnalyticsDbContext).Assembly,
            type => type.Namespace?.Contains("Analytics.Infrastructure.Persistence.Configurations") ?? false);

        base.OnModelCreating(modelBuilder);
    }
}
