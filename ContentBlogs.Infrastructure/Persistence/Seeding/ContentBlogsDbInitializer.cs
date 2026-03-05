using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentBlogs.Infrastructure.Persistence.Seeding;

public sealed class ContentBlogsDbInitializer(ContentBlogsDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid LanguageEnglish = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid LanguageArabic = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002");
    private static readonly Guid AuthorGuide = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TravelerOne = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid BlogId = Guid.Parse("b1b1b1b1-0000-0000-0000-000000000001");
    private static readonly Guid ParentCommentId = Guid.Parse("b1b1b1b1-0000-0000-0000-000000000002");
    private static readonly Guid ReplyCommentId = Guid.Parse("b1b1b1b1-0000-0000-0000-000000000003");
    private static readonly Guid ReactionId = Guid.Parse("b1b1b1b1-0000-0000-0000-000000000004");

    public int Order => 110;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Blogs.AnyAsync(cancellationToken))
        {
            return;
        }

        var blog = CreateBlog();
        var translations = CreateTranslations();
        var blogTours = CreateBlogTours();
        var comments = CreateComments();
        var reactions = CreateCommentReactions();

        dbContext.Blogs.Add(blog);
        dbContext.BlogTranslations.AddRange(translations);
        dbContext.BlogTours.AddRange(blogTours);
        dbContext.BlogComments.AddRange(comments);
        dbContext.BlogCommentReactions.AddRange(reactions);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Blog CreateBlog()
    {
        var blog = CreateEntity<Blog>();
        SetProperty(blog, nameof(Blog.Id), BlogId);
        SetProperty(blog, nameof(Blog.Title), "Petra Sunrise: Practical Tips for First-Time Visitors");
        SetProperty(blog, nameof(Blog.Slug), "petra-sunrise-practical-tips");
        SetProperty(blog, nameof(Blog.Content), "Start at the visitor center before 7:30 AM, carry two liters of water, and plan your route to include the Treasury and Royal Tombs before midday heat.");
        SetProperty(blog, nameof(Blog.Summary), "A practical route and packing list for Petra day visits.");
        SetProperty(blog, nameof(Blog.AuthorId), AuthorGuide);
        SetProperty(blog, nameof(Blog.Status), BlogStatus.Published);
        SetProperty(blog, nameof(Blog.IsFeatured), true);
        SetProperty(blog, nameof(Blog.ViewCount), 420);
        SetProperty(blog, nameof(Blog.ReadTimeMinutes), 6);
        SetProperty(blog, nameof(Blog.MetaTitle), "Petra Sunrise Guide | YallaJo");
        SetProperty(blog, nameof(Blog.MetaDescription), "Best route, timing, and essentials for a smooth Petra sunrise experience.");
        SetProperty(blog, nameof(Blog.PublishedAt), DateTime.UtcNow.AddDays(-5));
        SetProperty(blog, nameof(Blog.PlaceId), SeedContentIds.PlacePetra);
        return blog;
    }

    private static List<BlogTranslation> CreateTranslations()
    {
        var en = CreateEntity<BlogTranslation>();
        SetProperty(en, nameof(BlogTranslation.BlogId), BlogId);
        SetProperty(en, nameof(BlogTranslation.LanguageId), LanguageEnglish);
        SetProperty(en, nameof(BlogTranslation.Title), "Petra Sunrise: Practical Tips for First-Time Visitors");
        SetProperty(en, nameof(BlogTranslation.Content), "Arrive early, wear trail shoes, and keep your route simple for your first day in Petra.");
        SetProperty(en, nameof(BlogTranslation.Summary), "Practical Petra advice.");

        var ar = CreateEntity<BlogTranslation>();
        SetProperty(ar, nameof(BlogTranslation.BlogId), BlogId);
        SetProperty(ar, nameof(BlogTranslation.LanguageId), LanguageArabic);
        SetProperty(ar, nameof(BlogTranslation.Title), "Petra Sunrise: Practical Tips for First-Time Visitors");
        SetProperty(ar, nameof(BlogTranslation.Content), "Start early, stay hydrated, and prioritize key landmarks before peak crowd time.");
        SetProperty(ar, nameof(BlogTranslation.Summary), "Early Petra route planning tips.");

        return [en, ar];
    }

    private static List<BlogTour> CreateBlogTours()
    {
        var link = CreateEntity<BlogTour>();
        SetProperty(link, nameof(BlogTour.BlogId), BlogId);
        SetProperty(link, nameof(BlogTour.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(link, nameof(BlogTour.SortOrder), 1);
        return [link];
    }

    private static List<BlogComment> CreateComments()
    {
        var parent = CreateEntity<BlogComment>();
        SetProperty(parent, nameof(BlogComment.Id), ParentCommentId);
        SetProperty(parent, nameof(BlogComment.BlogId), BlogId);
        SetProperty(parent, nameof(BlogComment.UserId), TravelerOne);
        SetProperty(parent, nameof(BlogComment.Content), "Very useful timing notes. We avoided the midday crowd completely.");
        SetProperty(parent, nameof(BlogComment.LikeCount), 3);

        var reply = CreateEntity<BlogComment>();
        SetProperty(reply, nameof(BlogComment.Id), ReplyCommentId);
        SetProperty(reply, nameof(BlogComment.BlogId), BlogId);
        SetProperty(reply, nameof(BlogComment.ParentCommentId), ParentCommentId);
        SetProperty(reply, nameof(BlogComment.UserId), TravelerTwo);
        SetProperty(reply, nameof(BlogComment.Content), "Agree. Taking the monastery trail first made a big difference.");
        SetProperty(reply, nameof(BlogComment.LikeCount), 1);

        return [parent, reply];
    }

    private static List<BlogCommentReaction> CreateCommentReactions()
    {
        var reaction = CreateEntity<BlogCommentReaction>();
        SetProperty(reaction, nameof(BlogCommentReaction.Id), ReactionId);
        SetProperty(reaction, nameof(BlogCommentReaction.CommentId), ParentCommentId);
        SetProperty(reaction, nameof(BlogCommentReaction.UserId), TravelerTwo);
        SetProperty(reaction, nameof(BlogCommentReaction.ReactionType), ReactionType.Helpful);
        return [reaction];
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
        {
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        }

        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
