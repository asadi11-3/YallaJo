using System.Reflection;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentBlogs.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only FE-2D smoke seeder for BLOG / CREATOR UI states.
///
/// Distinct from <see cref="BlogTestDataDbInitializer"/> (which seeds admin-authored "Admin UI"
/// blogs): this seeder provisions a real <see cref="CreatorProfile"/> (so the Creator area, public
/// creator page, and creator-authored articles can be exercised) plus clearly-labelled
/// ("FE2D Smoke Blog …") creator-owned articles in every lifecycle status, with comment states on
/// the published/full article.
///
/// States covered:
///   • Creator profile + backing (Approved) application — userA becomes a creator.
///   • FE2D Smoke Blog Full       — Published, place-linked, EN+AR translations, several comments
///                                  (owner / other user / redacted-"deleted" / reply), a reaction, view count.
///   • FE2D Smoke Blog Minimal    — Published, required fields only, no comments/reactions/place link.
///   • FE2D Smoke Blog Draft      — creator's draft (My Articles draft tab).
///   • FE2D Smoke Blog Pending    — submitted, awaiting review (creator pending + admin queue).
///   • FE2D Smoke Blog Rejected   — rejected with reason (creator rejected tab + admin queue).
///   • FE2D Smoke Blog Published  — a second published creator article (public list has multiple items).
///
/// Safety / conventions:
///   • Invoked only from <c>UseDataSeedingAsync</c> (Development-gated; or explicit <c>Seeding:Enabled</c>).
///   • No production data, no real PII (fictional FE2D creator/content only).
///   • Idempotent: per-slug guard via <c>IgnoreQueryFilters()</c>; creator/comments guarded by id existence.
///   • Domain factory + reflection for deterministic Id/Status (matching the existing blog seeders);
///     RowVersion is left for SQL Server to generate; domain events cleared (no outbox side-effects).
///   • Respects unique indexes: Blog.Slug (global), CreatorProfile.UserId + .Slug, (BlogId, LanguageId).
///   • Order 117 — after ContentBlogsDbInitializer (110) and BlogTestDataDbInitializer (115).
///
/// Backend limitations (documented, NOT worked around):
///   • No dedicated Blog cover/article-image entity — images are Attachments-module rows, which a
///     ContentBlogs seeder cannot create. The "article with images" creator state is therefore not
///     seedable here (must be added through the Creator image-upload UI). All seeded blogs are
///     image-less (which also covers the "blog without images" / minimal state).
///   • There is no separate blog "tags/categories" entity in ContentBlogs (handled externally).
///   • BlogComment soft-delete uses IsContentRedacted (not IsDeleted) — the "deleted comment" state
///     is seeded by setting IsContentRedacted=true and Content="[deleted]".
/// </summary>
public sealed class Fe2dSmokeBlogSeeder(
    ContentBlogsDbContext dbContext,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<Fe2dSmokeBlogSeeder> logger) : IModuleDbInitializer
{
    // ── Users (reuse existing seeded identities) ─────────────────────────────
    private static readonly Guid CreatorUser = Guid.Parse("b0000000-0000-0000-0000-000000000002"); // userA@yallajo.test → the creator
    private static readonly Guid OtherUser   = Guid.Parse("b0000000-0000-0000-0000-000000000003"); // userB@yallajo.test → other commenter
    private static readonly Guid TravelerOne = Guid.Parse("66666666-6666-6666-6666-666666666666"); // traveler.1 → reactor / commenter
    private static readonly Guid Admin1      = Guid.Parse("33333333-3333-3333-3333-333333333333"); // admin.1 → reviewer (rejection)

    // ── Deterministic ids ────────────────────────────────────────────────────
    private static readonly Guid CreatorAppId     = Guid.Parse("fe2d0000-0000-0000-0000-0000000000c0");
    private static readonly Guid CreatorProfileId = Guid.Parse("fe2d0000-0000-0000-0000-0000000000c1");

    private static readonly Guid BlogFullId      = Guid.Parse("fe2d0000-0000-0000-0000-0000000000d1");
    private static readonly Guid BlogMinimalId   = Guid.Parse("fe2d0000-0000-0000-0000-0000000000d2");
    private static readonly Guid BlogDraftId     = Guid.Parse("fe2d0000-0000-0000-0000-0000000000d3");
    private static readonly Guid BlogPendingId   = Guid.Parse("fe2d0000-0000-0000-0000-0000000000d4");
    private static readonly Guid BlogRejectedId  = Guid.Parse("fe2d0000-0000-0000-0000-0000000000d5");
    private static readonly Guid BlogPublished2Id= Guid.Parse("fe2d0000-0000-0000-0000-0000000000d6");

    private static readonly Guid CommentOwnerId    = Guid.Parse("fe2d0000-0000-0000-0000-0000000000e1");
    private static readonly Guid CommentOtherId    = Guid.Parse("fe2d0000-0000-0000-0000-0000000000e2");
    private static readonly Guid CommentReplyId    = Guid.Parse("fe2d0000-0000-0000-0000-0000000000e3");
    private static readonly Guid CommentRedactedId = Guid.Parse("fe2d0000-0000-0000-0000-0000000000e4");

    private static readonly Guid SmokePlaceFull = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b1"); // FE2D Smoke Place Full

    public const string CreatorSlug   = "fe2d-smoke-creator";
    public const string SlugFull      = "fe2d-smoke-blog-full";
    public const string SlugMinimal   = "fe2d-smoke-blog-minimal";
    public const string SlugDraft     = "fe2d-smoke-blog-draft";
    public const string SlugPending   = "fe2d-smoke-blog-pending";
    public const string SlugRejected  = "fe2d-smoke-blog-rejected";
    public const string SlugPublished = "fe2d-smoke-blog-published";

    private const string EnContent =
        "FE2D smoke-test article body. This development-only content is long enough to pass blog content " +
        "validation and lets a tester exercise the public blog detail page, the creator article editor, " +
        "status transitions, comments and reactions, and admin moderation — all without touching production data.";

    private const string ArContent =
        "نص مقال تجريبي من FE2D لأغراض التطوير فقط. يحتوي على محتوى كافٍ لاختبار صفحة تفاصيل المدونة والتعليقات " +
        "والتفاعلات وإجراءات المراجعة دون التأثير على بيانات الإنتاج.";

    public int Order => 117;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var languages = await activeLanguageProvider
            .GetActiveLanguagesAsync(cancellationToken)
            .ConfigureAwait(false);

        var en = languages.FirstOrDefault(l => string.Equals(l.Code, "en", StringComparison.OrdinalIgnoreCase));
        var ar = languages.FirstOrDefault(l => string.Equals(l.Code, "ar", StringComparison.OrdinalIgnoreCase));

        if (en is null)
        {
            logger.LogWarning("Fe2dSmokeBlogSeeder: English (en) language not found — skipping FE2D blog seeding.");
            return;
        }

        var now = DateTime.UtcNow;

        await SeedCreatorAsync(cancellationToken).ConfigureAwait(false);

        var blogs = new List<Blog>();
        var translations = new List<BlogTranslation>();
        var tours = new List<BlogTour>();
        var comments = new List<BlogComment>();
        var reactions = new List<BlogCommentReaction>();

        // ── 1. FULL published, creator-authored, place-linked, with comments ──
        if (await ShouldSeedAsync(SlugFull, cancellationToken))
        {
            var blog = BuildCreatorBlog(BlogFullId, "FE2D Smoke Blog Full", SlugFull, en.Id, now,
                status: BlogStatus.Published, placeId: SmokePlaceFull, summary: "Full FE2D smoke blog with comments.");
            SetProperty(blog, nameof(Blog.PublishedAt), now.AddDays(-3));
            SetProperty(blog, nameof(Blog.ViewCount), 128);
            SetProperty(blog, nameof(Blog.CommentCount), 4);
            SetProperty(blog, nameof(Blog.ReactionCount), 1);
            blogs.Add(blog);

            translations.Add(BlogTranslation.Create(BlogFullId, en.Id, "FE2D Smoke Blog Full", EnContent, "Full FE2D smoke blog."));
            if (ar is not null)
                translations.Add(BlogTranslation.Create(BlogFullId, ar.Id, "مدونة FE2D كاملة", ArContent, "مدونة FE2D تجريبية كاملة."));

            // Related tour link (Petra Explorer seed tour).
            tours.Add(BlogTour.Create(BlogFullId, Guid.Parse("ffffffff-0000-0000-0000-000000000001"), 0));

            // Comments: owner / other-user / reply / redacted("deleted").
            comments.Add(BuildComment(CommentOwnerId, BlogFullId, CreatorUser, null,
                "FE2D comment owned by the creator/current user.", now.AddHours(-30)));
            comments.Add(BuildComment(CommentOtherId, BlogFullId, OtherUser, null,
                "FE2D comment owned by another user (userB).", now.AddHours(-26)));
            comments.Add(BuildComment(CommentReplyId, BlogFullId, TravelerOne, CommentOtherId,
                "FE2D reply by traveler.1 to userB's comment.", now.AddHours(-20)));
            var redacted = BuildComment(CommentRedactedId, BlogFullId, TravelerOne, null,
                BlogComment.RedactedContentMarker, now.AddHours(-40));
            SetProperty(redacted, nameof(BlogComment.IsContentRedacted), true);
            comments.Add(redacted);

            // A reaction on the owner comment.
            reactions.Add(BuildReaction(CommentOwnerId, OtherUser, ReactionType.Helpful, now.AddHours(-25)));
        }

        // ── 2. MINIMAL published (no comments / no place / no AR translation) ─
        if (await ShouldSeedAsync(SlugMinimal, cancellationToken))
        {
            var blog = BuildCreatorBlog(BlogMinimalId, "FE2D Smoke Blog Minimal", SlugMinimal, en.Id, now,
                status: BlogStatus.Published, placeId: null, summary: null);
            SetProperty(blog, nameof(Blog.PublishedAt), now.AddDays(-1));
            blogs.Add(blog);
            translations.Add(BlogTranslation.Create(BlogMinimalId, en.Id, "FE2D Smoke Blog Minimal", EnContent, null));
        }

        // ── 3. DRAFT (creator-owned) ─────────────────────────────────────────
        if (await ShouldSeedAsync(SlugDraft, cancellationToken))
        {
            var blog = BuildCreatorBlog(BlogDraftId, "FE2D Smoke Blog Draft", SlugDraft, en.Id, now,
                status: BlogStatus.Draft, placeId: null, summary: "Draft FE2D smoke article.");
            blogs.Add(blog);
            translations.Add(BlogTranslation.Create(BlogDraftId, en.Id, "FE2D Smoke Blog Draft", EnContent, "Draft."));
        }

        // ── 4. PENDING review (creator-owned, submitted) ─────────────────────
        if (await ShouldSeedAsync(SlugPending, cancellationToken))
        {
            var blog = BuildCreatorBlog(BlogPendingId, "FE2D Smoke Blog Pending", SlugPending, en.Id, now,
                status: BlogStatus.PendingReview, placeId: null, summary: "Pending FE2D smoke article.");
            SetProperty(blog, nameof(Blog.SubmittedAt), now.AddHours(-6));
            blogs.Add(blog);
            translations.Add(BlogTranslation.Create(BlogPendingId, en.Id, "FE2D Smoke Blog Pending", EnContent, "Pending."));
        }

        // ── 5. REJECTED (creator-owned, with reason) ─────────────────────────
        if (await ShouldSeedAsync(SlugRejected, cancellationToken))
        {
            var blog = BuildCreatorBlog(BlogRejectedId, "FE2D Smoke Blog Rejected", SlugRejected, en.Id, now,
                status: BlogStatus.Rejected, placeId: null, summary: "Rejected FE2D smoke article.");
            SetProperty(blog, nameof(Blog.SubmittedAt), now.AddDays(-2));
            SetProperty(blog, nameof(Blog.ReviewedAt), now.AddDays(-1));
            SetProperty(blog, nameof(Blog.ReviewedByAdminId), Admin1);
            SetProperty(blog, nameof(Blog.RejectionReason), "FE2D test rejection: needs more detail and sources.");
            blogs.Add(blog);
            translations.Add(BlogTranslation.Create(BlogRejectedId, en.Id, "FE2D Smoke Blog Rejected", EnContent, "Rejected."));
        }

        // ── 6. PUBLISHED #2 (so the public list has multiple creator items) ──
        if (await ShouldSeedAsync(SlugPublished, cancellationToken))
        {
            var blog = BuildCreatorBlog(BlogPublished2Id, "FE2D Smoke Blog Published", SlugPublished, en.Id, now,
                status: BlogStatus.Published, placeId: null, summary: "Second published FE2D smoke article.");
            SetProperty(blog, nameof(Blog.PublishedAt), now.AddDays(-2));
            SetProperty(blog, nameof(Blog.ViewCount), 42);
            blogs.Add(blog);
            translations.Add(BlogTranslation.Create(BlogPublished2Id, en.Id, "FE2D Smoke Blog Published", EnContent, "Published."));
        }

        if (blogs.Count == 0)
        {
            logger.LogInformation("Fe2dSmokeBlogSeeder: all FE2D blogs already present — nothing to seed.");
            return;
        }

        foreach (var blog in blogs)
            blog.ClearDomainEvents();
        foreach (var c in comments)
            c.ClearDomainEvents();

        dbContext.Blogs.AddRange(blogs);
        dbContext.BlogTranslations.AddRange(translations);
        if (tours.Count > 0) dbContext.BlogTours.AddRange(tours);
        if (comments.Count > 0) dbContext.BlogComments.AddRange(comments);
        if (reactions.Count > 0) dbContext.BlogCommentReactions.AddRange(reactions);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Fe2dSmokeBlogSeeder: seeded {Blogs} FE2D blog(s), {Comments} comment(s) (creator={CreatorId}).",
            blogs.Count, comments.Count, CreatorProfileId);
    }

    private async Task SeedCreatorAsync(CancellationToken ct)
    {
        var exists = await dbContext.CreatorProfiles
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == CreatorProfileId || c.UserId == CreatorUser || c.Slug == CreatorSlug, ct)
            .ConfigureAwait(false);

        if (exists)
            return;

        var now = DateTime.UtcNow;

        // Backing (Approved) application — keeps CreatorProfile.ApplicationId consistent.
        if (!await dbContext.CreatorApplications.IgnoreQueryFilters().AnyAsync(a => a.Id == CreatorAppId, ct).ConfigureAwait(false))
        {
            var app = CreateEntity<CreatorApplication>();
            SetProperty(app, nameof(CreatorApplication.Id), CreatorAppId);
            SetProperty(app, nameof(CreatorApplication.ApplicantUserId), CreatorUser);
            SetProperty(app, nameof(CreatorApplication.Status), CreatorApplicationStatus.Approved);
            SetProperty(app, nameof(CreatorApplication.Source), CreatorApplicationSource.SelfApplied);
            SetProperty(app, nameof(CreatorApplication.Bio), "FE2D smoke creator application (dev only).");
            SetProperty(app, nameof(CreatorApplication.CreatedAt), now.AddDays(-10));
            dbContext.CreatorApplications.Add(app);
        }

        var profile = CreateEntity<CreatorProfile>();
        SetProperty(profile, nameof(CreatorProfile.Id), CreatorProfileId);
        SetProperty(profile, nameof(CreatorProfile.UserId), CreatorUser);
        SetProperty(profile, nameof(CreatorProfile.ApplicationId), CreatorAppId);
        SetProperty(profile, nameof(CreatorProfile.Slug), CreatorSlug);
        SetProperty(profile, nameof(CreatorProfile.DisplayName), "FE2D Smoke Creator");
        SetProperty(profile, nameof(CreatorProfile.Bio), "FE2D development-only content creator used for smoke testing.");
        SetProperty(profile, nameof(CreatorProfile.TrustTier), CreatorTrustTier.New);
        SetProperty(profile, nameof(CreatorProfile.Status), CreatorProfileStatus.Active);
        SetProperty(profile, nameof(CreatorProfile.CreatedAt), now.AddDays(-9));
        dbContext.CreatorProfiles.Add(profile);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<bool> ShouldSeedAsync(string slug, CancellationToken ct)
    {
        var exists = await dbContext.Blogs
            .IgnoreQueryFilters()
            .AnyAsync(b => b.Slug == slug, ct)
            .ConfigureAwait(false);
        return !exists;
    }

    private static Blog BuildCreatorBlog(
        Guid id, string title, string slug, Guid languageId, DateTime nowUtc,
        BlogStatus status, Guid? placeId, string? summary)
    {
        var blog = Blog.CreateByCreator(
            title: title,
            slug: slug,
            content: EnContent,
            authorId: CreatorUser,
            creatorProfileId: CreatorProfileId,
            sourceLanguageId: languageId,
            utcNow: nowUtc,
            summary: summary,
            placeId: placeId);

        SetProperty(blog, nameof(Blog.Id), id);
        SetProperty(blog, nameof(Blog.Status), status);
        return blog;
    }

    private static BlogComment BuildComment(Guid id, Guid blogId, Guid userId, Guid? parentId, string content, DateTime createdAt)
    {
        var comment = CreateEntity<BlogComment>();
        SetProperty(comment, nameof(BlogComment.Id), id);
        SetProperty(comment, nameof(BlogComment.BlogId), blogId);
        SetProperty(comment, nameof(BlogComment.UserId), userId);
        SetProperty(comment, nameof(BlogComment.ParentCommentId), parentId);
        SetProperty(comment, nameof(BlogComment.Content), content);
        SetProperty(comment, nameof(BlogComment.IsContentRedacted), false);
        SetProperty(comment, nameof(BlogComment.CreatedAt), createdAt);
        return comment;
    }

    private static BlogCommentReaction BuildReaction(Guid commentId, Guid userId, ReactionType type, DateTime createdAt)
    {
        // BlogCommentReaction.Create is internal; build via reflection to keep the seeder self-contained.
        var reaction = CreateEntity<BlogCommentReaction>();
        SetProperty(reaction, nameof(BlogCommentReaction.Id), Guid.CreateVersion7());
        SetProperty(reaction, nameof(BlogCommentReaction.CommentId), commentId);
        SetProperty(reaction, nameof(BlogCommentReaction.UserId), userId);
        SetProperty(reaction, nameof(BlogCommentReaction.ReactionType), type);
        TrySetProperty(reaction, "CreatedAt", createdAt);
        return reaction;
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property is null)
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");

        property.SetValue(target, value);
    }

    private static void TrySetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(target, value);
    }
}
