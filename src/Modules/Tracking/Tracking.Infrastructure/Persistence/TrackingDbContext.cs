using Microsoft.EntityFrameworkCore;
using Tracking.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Tracking.Infrastructure.Persistence;

public sealed class TrackingDbContext : DbContext, IDbContext
{
    public TrackingDbContext(DbContextOptions<TrackingDbContext> options) : base(options) { }

    public DbSet<LiveTrackingSession> LiveTrackingSessions => Set<LiveTrackingSession>();
    public DbSet<LocationSnapshot> LocationSnapshots => Set<LocationSnapshot>();
    public DbSet<TourCheckpoint> TourCheckpoints => Set<TourCheckpoint>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tracking");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TrackingDbContext).Assembly,
            type => type.Namespace?.Contains("Tracking.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
