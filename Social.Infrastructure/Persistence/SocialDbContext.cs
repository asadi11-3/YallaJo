using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Social.Infrastructure.Persistence;

public sealed class SocialDbContext : DbContext, IDbContext
{
    public SocialDbContext(DbContextOptions<SocialDbContext> options) : base(options) { }

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ContentModerationLog> ContentModerationLogs => Set<ContentModerationLog>();
    public DbSet<AccessibilityReview> AccessibilityReviews => Set<AccessibilityReview>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("social");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SocialDbContext).Assembly,
            type => type.Namespace?.Contains("Social.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
