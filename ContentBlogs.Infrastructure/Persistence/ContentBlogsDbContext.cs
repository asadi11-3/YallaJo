using ContentBlogs.Domain.Entities;
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
