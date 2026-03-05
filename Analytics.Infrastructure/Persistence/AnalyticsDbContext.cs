using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
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
    public DbSet<RecommendationCache> RecommendationCaches => Set<RecommendationCache>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("analytics");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AnalyticsDbContext).Assembly,
            type => type.Namespace?.Contains("Analytics.Infrastructure.Persistence.Configurations") ?? false);

        base.OnModelCreating(modelBuilder);
    }
}
