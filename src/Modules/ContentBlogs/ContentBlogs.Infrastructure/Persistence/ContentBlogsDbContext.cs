using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Entities.Creators;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.Persistence;

public sealed class ContentBlogsDbContext : DbContext, IDbContext
{
    public ContentBlogsDbContext(DbContextOptions<ContentBlogsDbContext> options) : base(options) { }

    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogTranslation> BlogTranslations => Set<BlogTranslation>();
    public DbSet<BlogTour> BlogTours => Set<BlogTour>();
    public DbSet<BlogComment> BlogComments => Set<BlogComment>();
    public DbSet<BlogCommentReaction> BlogCommentReactions => Set<BlogCommentReaction>();
    public DbSet<BlogView> BlogViews => Set<BlogView>();

    // ── Creator Identity ─────────────────────────────────────────────────
    public DbSet<CreatorApplication> CreatorApplications => Set<CreatorApplication>();
    public DbSet<CreatorProfile> CreatorProfiles => Set<CreatorProfile>();
    public DbSet<CreatorInvitation> CreatorInvitations => Set<CreatorInvitation>();
    public DbSet<CreatorFollow> CreatorFollows => Set<CreatorFollow>();
    public DbSet<CreatorNiche> CreatorNiches => Set<CreatorNiche>();
    // CreatorPost removed — merged into Blog entity (BlogCreatorPost-Merger plan)

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content_blogs");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ContentBlogsDbContext).Assembly,
            type => type.Namespace?.Contains("ContentBlogs.Infrastructure.Persistence.Configurations") ?? false);
        base.OnModelCreating(modelBuilder);
    }
}
