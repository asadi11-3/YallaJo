using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentBlogs.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-ONLY seed initializer that creates a small, deterministic set of Blog rows in
/// every status so the Admin Blog Management UI can be exercised end to end.
/// <para>
/// Runs through the standard seeding pipeline, which is already gated by
/// <c>IsDevelopment()</c> / <c>Seeding:Enabled</c> — never in production. It is
/// per-slug idempotent (existing slugs are skipped), so re-runs never duplicate rows.
/// </para>
/// <para>
/// All blogs are authored by the seeded <b>admin.1</b> user so the blog
/// author-hierarchy guard's self-management branch always permits admin.1 to manage
/// them. Language ids are resolved at runtime from <see cref="IActiveLanguageProvider"/>
/// (never hardcoded). Published/Hidden blogs receive a real Arabic translation so the
/// publish/hide/unhide language gates are satisfied.
/// </para>
/// </summary>
public sealed class BlogTestDataDbInitializer(
    ContentBlogsDbContext dbContext,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<BlogTestDataDbInitializer> logger) : IModuleDbInitializer
{
    // admin.1@yallajo.local (Admin tier) — see Auth seed users.
    private static readonly Guid Admin1 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Deterministic blog ids so re-runs and manual testing are stable.
    private static readonly Guid DraftBlogId    = Guid.Parse("b1b1b1b1-0000-0000-0000-0000000000a1");
    private static readonly Guid PendingBlogId  = Guid.Parse("b1b1b1b1-0000-0000-0000-0000000000a2");
    private static readonly Guid PublishedBlogId= Guid.Parse("b1b1b1b1-0000-0000-0000-0000000000a3");
    private static readonly Guid HiddenBlogId   = Guid.Parse("b1b1b1b1-0000-0000-0000-0000000000a4");
    private static readonly Guid DeletedBlogId  = Guid.Parse("b1b1b1b1-0000-0000-0000-0000000000a5");
    private static readonly Guid RemovedBlogId  = Guid.Parse("b1b1b1b1-0000-0000-0000-0000000000a6");

    private const string EnContent =
        "This is a development testing blog created for validating the Admin Blog Management UI. " +
        "It contains enough text to pass content validation and allows testing create, edit, status " +
        "transitions, moderation actions, concurrency RowVersion handling, and list tab behavior safely " +
        "in the local development database.";

    private const string ArContent =
        "هذا مقال تجريبي مخصص لاختبار واجهة إدارة المقالات في لوحة التحكم. يحتوي هذا النص على محتوى كاف " +
        "لاختبار عمليات النشر والمراجعة والإخفاء والاستعادة والتعديل دون التأثير على بيانات الإنتاج.";

    // After the main ContentBlogsDbInitializer (Order 110).
    public int Order => 115;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // ── Resolve EN/AR language ids at runtime (never hardcoded) ──────────────
        var languages = await activeLanguageProvider
            .GetActiveLanguagesAsync(cancellationToken)
            .ConfigureAwait(false);

        var en = languages.FirstOrDefault(l => string.Equals(l.Code, "en", StringComparison.OrdinalIgnoreCase));
        var ar = languages.FirstOrDefault(l => string.Equals(l.Code, "ar", StringComparison.OrdinalIgnoreCase));

        if (en is null)
        {
            logger.LogWarning(
                "BlogTestDataDbInitializer: English (en) language not found — skipping blog test-data seeding.");
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var toAdd = new List<Blog>();
        var translations = new List<BlogTranslation>();

        // 1. Draft (EN only)
        if (await ShouldSeedAsync("test-draft-blog-admin-ui", cancellationToken))
        {
            var blog = BuildBlog(DraftBlogId, "Test Draft Blog - Admin UI", "test-draft-blog-admin-ui", en.Id, BlogStatus.Draft, nowUtc);
            toAdd.Add(blog);
            translations.Add(BlogTranslation.Create(DraftBlogId, en.Id, "Test Draft Blog - Admin UI", EnContent, "Draft test blog."));
        }

        // 2. PendingReview (EN only)
        if (await ShouldSeedAsync("test-pending-review-blog-admin-ui", cancellationToken))
        {
            var blog = BuildBlog(PendingBlogId, "Test Pending Review Blog - Admin UI", "test-pending-review-blog-admin-ui", en.Id, BlogStatus.PendingReview, nowUtc);
            SetProperty(blog, nameof(Blog.SubmittedAt), nowUtc);
            toAdd.Add(blog);
            translations.Add(BlogTranslation.Create(PendingBlogId, en.Id, "Test Pending Review Blog - Admin UI", EnContent, "Pending review test blog."));
        }

        // 3. Published (EN + AR; PublishedAt set)
        if (await ShouldSeedAsync("test-published-blog-admin-ui", cancellationToken))
        {
            var blog = BuildBlog(PublishedBlogId, "Test Published Blog - Admin UI", "test-published-blog-admin-ui", en.Id, BlogStatus.Published, nowUtc);
            SetProperty(blog, nameof(Blog.PublishedAt), nowUtc.AddDays(-2));
            toAdd.Add(blog);
            translations.Add(BlogTranslation.Create(PublishedBlogId, en.Id, "Test Published Blog - Admin UI", EnContent, "Published test blog."));
            if (ar is not null)
                translations.Add(BlogTranslation.Create(PublishedBlogId, ar.Id, "مقال منشور تجريبي - واجهة الإدارة", ArContent, "مقال تجريبي منشور."));
            else
                LogMissingArabic("test-published-blog-admin-ui");
        }

        // 4. Hidden (EN + AR)
        if (await ShouldSeedAsync("test-hidden-blog-admin-ui", cancellationToken))
        {
            var blog = BuildBlog(HiddenBlogId, "Test Hidden Blog - Admin UI", "test-hidden-blog-admin-ui", en.Id, BlogStatus.Hidden, nowUtc);
            SetProperty(blog, nameof(Blog.PublishedAt), nowUtc.AddDays(-3));
            toAdd.Add(blog);
            translations.Add(BlogTranslation.Create(HiddenBlogId, en.Id, "Test Hidden Blog - Admin UI", EnContent, "Hidden test blog."));
            if (ar is not null)
                translations.Add(BlogTranslation.Create(HiddenBlogId, ar.Id, "مقال مخفي تجريبي - واجهة الإدارة", ArContent, "مقال تجريبي مخفي."));
            else
                LogMissingArabic("test-hidden-blog-admin-ui");
        }

        // 5. Deleted (Draft + soft-deleted)
        if (await ShouldSeedAsync("test-deleted-blog-admin-ui", cancellationToken))
        {
            var blog = BuildBlog(DeletedBlogId, "Test Deleted Blog - Admin UI", "test-deleted-blog-admin-ui", en.Id, BlogStatus.Draft, nowUtc);
            SetProperty(blog, nameof(Blog.IsDeleted), true);
            SetProperty(blog, nameof(Blog.DeletedAt), nowUtc.AddDays(-1));
            toAdd.Add(blog);
            translations.Add(BlogTranslation.Create(DeletedBlogId, en.Id, "Test Deleted Blog - Admin UI", EnContent, "Deleted test blog."));
        }

        // 6. Removed (EN only)
        if (await ShouldSeedAsync("test-removed-blog-admin-ui", cancellationToken))
        {
            var blog = BuildBlog(RemovedBlogId, "Test Removed Blog - Admin UI", "test-removed-blog-admin-ui", en.Id, BlogStatus.Removed, nowUtc);
            toAdd.Add(blog);
            translations.Add(BlogTranslation.Create(RemovedBlogId, en.Id, "Test Removed Blog - Admin UI", EnContent, "Removed test blog."));
        }

        if (toAdd.Count == 0)
        {
            logger.LogInformation("BlogTestDataDbInitializer: all test blogs already present — nothing to seed.");
            return;
        }

        // Seeded rows must not dispatch domain events (no outbox side-effects on seed).
        foreach (var blog in toAdd)
            blog.ClearDomainEvents();

        dbContext.Blogs.AddRange(toAdd);
        dbContext.BlogTranslations.AddRange(translations);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "BlogTestDataDbInitializer: seeded {Count} Admin-UI test blog(s) (en={EnId}, ar={ArId}).",
            toAdd.Count, en.Id, ar?.Id);
    }

    private async Task<bool> ShouldSeedAsync(string slug, CancellationToken ct)
    {
        var exists = await dbContext.Blogs
            .IgnoreQueryFilters() // include soft-deleted so the deleted test blog isn't re-inserted
            .AnyAsync(b => b.Slug == slug, ct)
            .ConfigureAwait(false);

        if (exists)
            logger.LogInformation("BlogTestDataDbInitializer: blog '{Slug}' already exists — skipping.", slug);

        return !exists;
    }

    private void LogMissingArabic(string slug) =>
        logger.LogWarning(
            "BlogTestDataDbInitializer: Arabic (ar) language not found — '{Slug}' seeded without an AR translation; " +
            "publish/hide/unhide language gate may not be satisfiable.", slug);

    private static Blog BuildBlog(Guid id, string title, string slug, Guid languageId, BlogStatus status, DateTime nowUtc)
    {
        // Use the domain factory for validity, then set the deterministic id + target
        // status via reflection (Status has a non-public setter — same technique as the
        // existing ContentBlogsDbInitializer). RowVersion is intentionally NOT set so
        // SQL Server generates it on insert.
        var blog = Blog.Create(
            title:            title,
            slug:             slug,
            content:          EnContent,
            authorId:         Admin1,
            sourceLanguageId: languageId,
            utcNow:           nowUtc,
            summary:          "Admin UI test blog.");

        SetProperty(blog, nameof(Blog.Id), id);
        SetProperty(blog, nameof(Blog.Status), status);
        return blog;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");

        property.SetValue(target, value);
    }
}
