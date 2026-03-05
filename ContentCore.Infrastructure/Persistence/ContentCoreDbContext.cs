using ContentCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.Persistence;

public sealed class ContentCoreDbContext : DbContext, IDbContext
{
    public ContentCoreDbContext(DbContextOptions<ContentCoreDbContext> options) : base(options) { }

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<EntityImage> EntityImages => Set<EntityImage>();
    public DbSet<EntityCategory> EntityCategories => Set<EntityCategory>();
    public DbSet<EntityTag> EntityTags => Set<EntityTag>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content_core");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ContentCoreDbContext).Assembly,
            type => type.Namespace?.Contains("ContentCore.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
