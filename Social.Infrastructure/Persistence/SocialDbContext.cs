using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Social.Infrastructure.Persistence;

public sealed class SocialDbContext : DbContext, IDbContext
{
    public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options) { }

    // ── Aggregates ────────────────────────────────────────────────────────────
    public DbSet<Review> Reviews                                 => Set<Review>();
    public DbSet<Favorite> Favorites                             => Set<Favorite>();
    public DbSet<Report> Reports                                 => Set<Report>();
    public DbSet<EntityRatingCache> EntityRatingCaches           => Set<EntityRatingCache>();

    // ── BaseEntity children (no aggregate root — EF discovers via configurations) ──
    public DbSet<ContentModerationLog> ContentModerationLogs     => Set<ContentModerationLog>();
    public DbSet<ProfanityBlocklistEntry> ProfanityBlocklistEntries => Set<ProfanityBlocklistEntry>();
    public DbSet<BookingEligibilitySnapshot> BookingEligibilitySnapshots => Set<BookingEligibilitySnapshot>();
    public DbSet<PlaceSnapshot> PlaceSnapshots                   => Set<PlaceSnapshot>();
    public DbSet<BusinessSnapshot> BusinessSnapshots             => Set<BusinessSnapshot>();
    public DbSet<TourSnapshot> TourSnapshots                     => Set<TourSnapshot>();

    // ── Infrastructure ─────────────────────────────────────────────────────────
    public DbSet<OutboxMessage> OutboxMessages                   => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages                     => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("social");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SocialDbContext).Assembly,
            type => type.Namespace?.Contains("Social.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
