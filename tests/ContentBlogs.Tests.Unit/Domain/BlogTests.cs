using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Events;
using FluentAssertions;
using YallaJo.Tests.Shared;

namespace ContentBlogs.Tests.Unit.Domain;

/// <summary>
/// Pure domain tests for the Blog aggregate state machine.
/// No infrastructure, no DB — asserts that the aggregate enforces
/// its own invariants and emits the correct domain events.
/// </summary>
public sealed class BlogTests : DomainTestBase
{
    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Blog_Create_SetsDraftStatus_AndAuthorId()
    {
        var authorId = Guid.NewGuid();

        var blog = TestBlogFactory.CreateDraft(authorId: authorId);

        blog.Status.Should().Be(BlogStatus.Draft);
        blog.AuthorId.Should().Be(authorId);
        blog.IsDeleted.Should().BeFalse();
        blog.PublishedAt.Should().BeNull();
        blog.ViewCount.Should().Be(0);
    }

    [Fact]
    public void Blog_Create_NormalizesSlug()
    {
        var blog = Blog.Create(
            title:            "Test Blog",
            slug:             "  Petra-SUNRISE  ",
            content:          new string('x', 200),
            authorId:         Guid.NewGuid(),
            sourceLanguageId: Guid.NewGuid(),
            utcNow:           DateTime.UtcNow);

        blog.Slug.Should().Be("petra-sunrise");
    }

    [Fact]
    public void Blog_Create_RaisesBlogCreatedDomainEvent()
    {
        var authorId = Guid.NewGuid();
        var langId = Guid.NewGuid();
        var placeId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);

        var blog = Blog.Create(
            title:            "Petra Sunrise Guide",
            slug:             "petra-sunrise-guide",
            content:          new string('x', 200),
            authorId:         authorId,
            sourceLanguageId: langId,
            utcNow:           now,
            placeId:          placeId);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogCreatedDomainEvent>(blog);
        evt.BlogId.Should().Be(blog.Id);
        evt.Slug.Should().Be("petra-sunrise-guide");
        evt.Title.Should().Be("Petra Sunrise Guide");
        evt.AuthorId.Should().Be(authorId);
        evt.SourceLanguageId.Should().Be(langId);
        evt.PlaceId.Should().Be(placeId);
        evt.CreatedAtUtc.Should().Be(now);
    }

    // ── Publish ──────────────────────────────────────────────────────────────

    [Fact]
    public void Blog_Publish_FromDraft_SetsPublishedAndPreservesFirstPublishedAt()
    {
        var t1 = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 2, 1,  9, 0, 0, DateTimeKind.Utc);

        var blog = TestBlogFactory.CreateDraft(utcNow: t1);
        blog.ClearDomainEvents();

        // First publish
        blog.Publish(t1);
        blog.Status.Should().Be(BlogStatus.Published);
        blog.PublishedAt.Should().Be(t1);

        // Cycle back to Draft
        blog.ClearDomainEvents();
        blog.Unpublish(t2);

        // Second publish — PublishedAt must NOT be overwritten
        blog.ClearDomainEvents();
        blog.Publish(t2);
        blog.Status.Should().Be(BlogStatus.Published);
        blog.PublishedAt.Should().Be(t1, "PublishedAt must reflect the first publication timestamp");
    }

    [Fact]
    public void Blog_Publish_WhenPublished_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreatePublished();

        var act = () => blog.Publish(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InvalidTransition*");
    }

    [Fact]
    public void Blog_Publish_WhenArchived_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreateArchived();

        var act = () => blog.Publish(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InvalidTransition*");
    }

    [Fact]
    public void Blog_Publish_RaisesBlogPublishedDomainEvent()
    {
        var now = new DateTime(2026, 3, 1, 6, 0, 0, DateTimeKind.Utc);
        var blog = TestBlogFactory.CreateDraft(utcNow: now);
        blog.ClearDomainEvents();

        blog.Publish(now);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogPublishedDomainEvent>(blog);
        evt.BlogId.Should().Be(blog.Id);
        evt.Slug.Should().Be(blog.Slug);
        evt.PublishedAtUtc.Should().Be(now);
    }

    // ── Unpublish ─────────────────────────────────────────────────────────────

    [Fact]
    public void Blog_Unpublish_FromPublished_ReturnsStatusToDraft()
    {
        var blog = TestBlogFactory.CreatePublished();
        blog.ClearDomainEvents();

        blog.Unpublish(DateTime.UtcNow);

        blog.Status.Should().Be(BlogStatus.Draft);
        DomainEventAssertions.ShouldContainDomainEvent<BlogUnpublishedDomainEvent>(blog);
    }

    [Fact]
    public void Blog_Unpublish_FromDraft_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreateDraft();

        var act = () => blog.Unpublish(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InvalidTransition*");
    }

    [Fact]
    public void Blog_Unpublish_PreservesPublishedAt()
    {
        var publishTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var blog = TestBlogFactory.CreateDraft();
        blog.ClearDomainEvents();
        blog.Publish(publishTime);

        blog.ClearDomainEvents();
        blog.Unpublish(DateTime.UtcNow);

        blog.PublishedAt.Should().Be(publishTime,
            "PublishedAt must be preserved after Unpublish");
    }

    // ── Archive ───────────────────────────────────────────────────────────────

    [Fact]
    public void Blog_Archive_FromPublished_SetsArchived()
    {
        var blog = TestBlogFactory.CreatePublished();
        blog.ClearDomainEvents();

        blog.Archive(DateTime.UtcNow);

        blog.Status.Should().Be(BlogStatus.Archived);
        DomainEventAssertions.ShouldContainDomainEvent<BlogArchivedDomainEvent>(blog);
    }

    [Fact]
    public void Blog_Archive_FromDraft_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreateDraft();

        var act = () => blog.Archive(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InvalidTransition*");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Blog_Update_WhenArchived_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreateArchived();
        var now = DateTime.UtcNow;

        var act = () => blog.Update(
            title:            blog.Title,
            slug:             blog.Slug,
            content:          blog.Content,
            summary:          blog.Summary,
            metaTitle:        blog.MetaTitle,
            metaDescription:  blog.MetaDescription,
            placeId:          blog.PlaceId,
            readTimeMinutes:  blog.ReadTimeMinutes,
            utcNow:           now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ArchivedReadOnly*");
    }

    [Fact]
    public void Blog_Update_RaisesBlogUpdatedDomainEvent_WithChangedFields()
    {
        var blog = TestBlogFactory.CreateDraft(title: "Original Title", slug: "original-slug");
        blog.ClearDomainEvents();
        var now = DateTime.UtcNow;

        blog.Update(
            title:           "Updated Title",
            slug:            "updated-slug",
            content:         blog.Content,
            summary:         blog.Summary,
            metaTitle:       blog.MetaTitle,
            metaDescription: blog.MetaDescription,
            placeId:         blog.PlaceId,
            readTimeMinutes: blog.ReadTimeMinutes,
            utcNow:          now);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogUpdatedDomainEvent>(blog);
        evt.OldSlug.Should().Be("original-slug");
        evt.NewSlug.Should().Be("updated-slug");
        evt.FieldsChanged.Should().Contain(nameof(Blog.Title));
        evt.FieldsChanged.Should().Contain(nameof(Blog.Slug));
        evt.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Blog_Update_NoChangesMade_DoesNotRaiseEvent()
    {
        var blog = TestBlogFactory.CreateDraft();
        blog.ClearDomainEvents();

        // Update with identical values — nothing should change
        blog.Update(
            title:           blog.Title,
            slug:            blog.Slug,
            content:         blog.Content,
            summary:         blog.Summary,
            metaTitle:       blog.MetaTitle,
            metaDescription: blog.MetaDescription,
            placeId:         blog.PlaceId,
            readTimeMinutes: blog.ReadTimeMinutes,
            utcNow:          DateTime.UtcNow);

        blog.DomainEvents.Should().BeEmpty(
            "no BlogUpdatedDomainEvent should be raised when nothing changed");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Blog_Delete_SoftDeletes_AndRaisesDeletedEvent()
    {
        var now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var blog = TestBlogFactory.CreateDraft();
        blog.ClearDomainEvents();

        blog.Delete(now);

        blog.IsDeleted.Should().BeTrue();
        blog.DeletedAt.Should().Be(now);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogDeletedDomainEvent>(blog);
        evt.BlogId.Should().Be(blog.Id);
        evt.DeletedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Blog_Delete_WhenAlreadyDeleted_IsIdempotent()
    {
        var blog = TestBlogFactory.CreateDeleted();
        blog.ClearDomainEvents();

        // Second delete — must not throw and must not raise another event
        blog.Delete(DateTime.UtcNow);

        blog.DomainEvents.Should().BeEmpty(
            "Delete is idempotent — no event should fire on an already-deleted blog");
    }

    // ── PublishedAt idempotency ───────────────────────────────────────────────

    [Fact]
    public void Blog_PublishedAt_NotOverwritten_OnSecondPublish()
    {
        var firstPublish = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var secondPublish = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var blog = TestBlogFactory.CreateDraft();
        blog.Publish(firstPublish);
        var capturedPublishedAt = blog.PublishedAt;

        blog.Unpublish(secondPublish);
        blog.Publish(secondPublish);

        blog.PublishedAt.Should().Be(capturedPublishedAt,
            "PublishedAt records the first publication time and must never be overwritten");
    }

    // ── Feature / Unfeature ───────────────────────────────────────────────────

    [Fact]
    public void Blog_MarkAsFeatured_FromPublished_SetsIsFeaturedAndRaisesEvent()
    {
        var blog = TestBlogFactory.CreatePublished();
        blog.ClearDomainEvents();
        var now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        blog.MarkAsFeatured(now);

        blog.IsFeatured.Should().BeTrue();
        blog.UpdatedAt.Should().Be(now);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogFeaturedDomainEvent>(blog);
        evt.BlogId.Should().Be(blog.Id);
        evt.Slug.Should().Be(blog.Slug);
        evt.FeaturedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Blog_MarkAsFeatured_FromDraft_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreateDraft();

        var act = () => blog.MarkAsFeatured(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InvalidTransition*");
        blog.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Blog_MarkAsFeatured_FromArchived_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreateArchived();

        var act = () => blog.MarkAsFeatured(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*InvalidTransition*");
        blog.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Blog_MarkAsFeatured_WhenAlreadyFeatured_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreatePublished();
        blog.MarkAsFeatured(DateTime.UtcNow);

        var act = () => blog.MarkAsFeatured(DateTime.UtcNow.AddSeconds(1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already featured*");
    }

    [Fact]
    public void Blog_MarkAsUnfeatured_FromFeaturedPublished_ClearsIsFeaturedAndRaisesEvent()
    {
        var blog = TestBlogFactory.CreatePublished();
        blog.MarkAsFeatured(DateTime.UtcNow);
        blog.ClearDomainEvents();
        var now = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);

        blog.MarkAsUnfeatured(now);

        blog.IsFeatured.Should().BeFalse();
        blog.UpdatedAt.Should().Be(now);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogUnfeaturedDomainEvent>(blog);
        evt.BlogId.Should().Be(blog.Id);
        evt.UnfeaturedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Blog_MarkAsUnfeatured_FromFeaturedArchived_Succeeds()
    {
        // D2: Unfeature is allowed even when the blog is Archived (admins must
        // be able to clear the flag on archived content).
        var blog = TestBlogFactory.CreatePublished();
        blog.MarkAsFeatured(DateTime.UtcNow);
        blog.Archive(DateTime.UtcNow.AddMinutes(1));
        blog.ClearDomainEvents();

        var act = () => blog.MarkAsUnfeatured(DateTime.UtcNow.AddMinutes(2));

        act.Should().NotThrow();
        blog.IsFeatured.Should().BeFalse();
        DomainEventAssertions.ShouldContainDomainEvent<BlogUnfeaturedDomainEvent>(blog);
    }

    [Fact]
    public void Blog_MarkAsUnfeatured_WhenNotFeatured_ThrowsInvalidTransition()
    {
        var blog = TestBlogFactory.CreatePublished();

        var act = () => blog.MarkAsUnfeatured(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not currently featured*");
    }

    [Fact]
    public void Blog_MarkAsFeatured_UpdatesUpdatedAt()
    {
        var blog = TestBlogFactory.CreatePublished();
        var before = blog.UpdatedAt;
        var now = (before ?? DateTime.UtcNow).AddDays(1);

        blog.MarkAsFeatured(now);

        blog.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Blog_MarkAsUnfeatured_UpdatesUpdatedAt()
    {
        var blog = TestBlogFactory.CreatePublished();
        blog.MarkAsFeatured(DateTime.UtcNow);
        var now = DateTime.UtcNow.AddDays(1);

        blog.MarkAsUnfeatured(now);

        blog.UpdatedAt.Should().Be(now);
    }
}
