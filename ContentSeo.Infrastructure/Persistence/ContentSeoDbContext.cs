using ContentSeo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentSeo.Infrastructure.Persistence;

public sealed class ContentSeoDbContext : DbContext, IDbContext
{
    public ContentSeoDbContext(DbContextOptions<ContentSeoDbContext> options) : base(options) { }

    public DbSet<SeoMetadata> SeoMetadata => Set<SeoMetadata>();
    public DbSet<WeatherCache> WeatherCaches => Set<WeatherCache>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<FaqItemTranslation> FaqItemTranslations => Set<FaqItemTranslation>();
    public DbSet<SitemapEntry> SitemapEntries => Set<SitemapEntry>();
    public DbSet<Redirect> Redirects => Set<Redirect>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content_seo");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ContentSeoDbContext).Assembly,
            type => type.Namespace?.Contains("ContentSeo.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
