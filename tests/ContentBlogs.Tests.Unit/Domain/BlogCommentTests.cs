using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Events;
using FluentAssertions;
using YallaJo.Tests.Shared;

namespace ContentBlogs.Tests.Unit.Domain;

/// <summary>
/// Pure domain tests for the <see cref="BlogComment"/> aggregate.
/// No infrastructure, no DB — asserts that the aggregate enforces its own
/// invariants and emits the correct domain events.
///
/// Covered rules:
///   • Comments are only allowed on Published blogs.
///   • Content length: 1..1000 chars (trimmed).
///   • Nesting depth ≤ 2 (root, reply, reply-to-reply).
///   • Parent must belong to the same blog as the child.
///   • Redaction is a soft-delete that replaces content with a marker.
///   • Edits are blocked on redacted comments.
///   • Reactions are aggregate-root operations: one per user per comment.
///   • Reactions on redacted comments are blocked (Add), but removal is allowed.
/// </summary>
public sealed class BlogCommentTests : DomainTestBase
{
    private const string ValidContent = "This is a thoughtful, valid comment body.";

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void BlogComment_Create_SetsCoreFields()
    {
        var blogId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        var comment = BlogComment.Create(
            blogId:     blogId,
            userId:     userId,
            content:    ValidContent,
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     now);

        comment.BlogId.Should().Be(blogId);
        comment.UserId.Should().Be(userId);
        comment.ParentCommentId.Should().BeNull();
        comment.Content.Should().Be(ValidContent);
        comment.IsContentRedacted.Should().BeFalse();
        comment.LikeCount.Should().Be(0);
        comment.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void BlogComment_Create_TrimsContent()
    {
        var comment = BlogComment.Create(
            blogId:     Guid.NewGuid(),
            userId:     Guid.NewGuid(),
            content:    "  trimmed comment text  ",
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     DateTime.UtcNow);

        comment.Content.Should().Be("trimmed comment text");
    }

    [Fact]
    public void BlogComment_Create_RaisesBlogCommentCreatedDomainEvent()
    {
        var blogId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc);

        var comment = BlogComment.Create(
            blogId:     blogId,
            userId:     userId,
            content:    ValidContent,
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     now);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogCommentCreatedDomainEvent>(comment);
        evt.CommentId.Should().Be(comment.Id);
        evt.BlogId.Should().Be(blogId);
        evt.UserId.Should().Be(userId);
        evt.ParentCommentId.Should().BeNull();
        evt.CreatedAtUtc.Should().Be(now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlogComment_Create_RejectsEmptyContent(string? content)
    {
        var act = () => BlogComment.Create(
            blogId:     Guid.NewGuid(),
            userId:     Guid.NewGuid(),
            content:    content!,
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void BlogComment_Create_RejectsTooLongContent()
    {
        var oversized = new string('x', BlogComment.MaxContentLength + 1);

        var act = () => BlogComment.Create(
            blogId:     Guid.NewGuid(),
            userId:     Guid.NewGuid(),
            content:    oversized,
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*cannot exceed {BlogComment.MaxContentLength} characters*");
    }

    [Theory]
    [InlineData(BlogStatus.Draft)]
    [InlineData(BlogStatus.Archived)]
    public void BlogComment_Create_RejectsNonPublishedBlog(BlogStatus blogStatus)
    {
        var act = () => BlogComment.Create(
            blogId:     Guid.NewGuid(),
            userId:     Guid.NewGuid(),
            content:    ValidContent,
            parent:     null,
            blogStatus: blogStatus,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BlogNotPublished*");
    }

    [Fact]
    public void BlogComment_Create_RejectsEmptyBlogId()
    {
        var act = () => BlogComment.Create(
            blogId:     Guid.Empty,
            userId:     Guid.NewGuid(),
            content:    ValidContent,
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<ArgumentException>().WithMessage("*BlogId*");
    }

    [Fact]
    public void BlogComment_Create_RejectsEmptyUserId()
    {
        var act = () => BlogComment.Create(
            blogId:     Guid.NewGuid(),
            userId:     Guid.Empty,
            content:    ValidContent,
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<ArgumentException>().WithMessage("*UserId*");
    }

    // ── Edit ──────────────────────────────────────────────────────────────────

    [Fact]
    public void BlogComment_Edit_UpdatesContent_AndRaisesUpdatedEvent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        comment.ClearDomainEvents();
        var now = DateTime.UtcNow.AddMinutes(1);

        comment.Edit("Edited body of the comment.", now);

        comment.Content.Should().Be("Edited body of the comment.");
        comment.UpdatedAt.Should().Be(now);
        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogCommentUpdatedDomainEvent>(comment);
        evt.CommentId.Should().Be(comment.Id);
        evt.BlogId.Should().Be(comment.BlogId);
        evt.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void BlogComment_Edit_WithSameContent_IsNoOp_NoEvent()
    {
        var comment = TestBlogCommentFactory.CreateRoot(content: ValidContent);
        comment.ClearDomainEvents();
        var updatedAtBefore = comment.UpdatedAt;

        comment.Edit(ValidContent, DateTime.UtcNow.AddHours(1));

        comment.DomainEvents.Should().BeEmpty(
            "no-op edits must NOT raise an event or bump UpdatedAt");
        comment.UpdatedAt.Should().Be(updatedAtBefore);
    }

    [Fact]
    public void BlogComment_Edit_RejectsRedactedComment()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        comment.Redact(DateTime.UtcNow);

        var act = () => comment.Edit("attempted edit", DateTime.UtcNow.AddMinutes(1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Redacted*");
    }

    [Fact]
    public void BlogComment_Edit_RejectsTooLongContent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var oversized = new string('x', BlogComment.MaxContentLength + 1);

        var act = () => comment.Edit(oversized, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Redact ────────────────────────────────────────────────────────────────

    [Fact]
    public void BlogComment_Redact_SetsIsContentRedacted_AndReplacesContent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        comment.ClearDomainEvents();
        var now = DateTime.UtcNow.AddMinutes(5);

        comment.Redact(now);

        comment.IsContentRedacted.Should().BeTrue();
        comment.Content.Should().Be(BlogComment.RedactedContentMarker);
        comment.UpdatedAt.Should().Be(now);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogCommentDeletedDomainEvent>(comment);
        evt.CommentId.Should().Be(comment.Id);
        evt.BlogId.Should().Be(comment.BlogId);
        evt.UserId.Should().Be(comment.UserId);
        evt.DeletedAtUtc.Should().Be(now);
    }

    [Fact]
    public void BlogComment_Redact_IsIdempotent_NoSecondEvent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        comment.Redact(DateTime.UtcNow);
        comment.ClearDomainEvents();
        var updatedAtAfterFirstRedact = comment.UpdatedAt;

        comment.Redact(DateTime.UtcNow.AddHours(1));

        comment.IsContentRedacted.Should().BeTrue();
        comment.UpdatedAt.Should().Be(updatedAtAfterFirstRedact,
            "second redact must not bump UpdatedAt");
        comment.DomainEvents.Should().BeEmpty(
            "redact is idempotent — second call must NOT raise another deleted event");
    }

    // ── Reply depth ───────────────────────────────────────────────────────────

    [Fact]
    public void BlogComment_Reply_AtDepth1_IsAllowed()
    {
        var root = TestBlogCommentFactory.CreateRoot();

        var reply = BlogComment.Create(
            blogId:     root.BlogId,
            userId:     Guid.NewGuid(),
            content:    "reply",
            parent:     root,
            blogStatus: BlogStatus.Published,
            utcNow:     DateTime.UtcNow);

        reply.ParentCommentId.Should().Be(root.Id);
    }

    [Fact]
    public void BlogComment_Reply_AtDepth2_IsAllowed()
    {
        var root = TestBlogCommentFactory.CreateRoot();
        var depth1 = TestBlogCommentFactory.CreateReply(root);

        var depth2 = TestBlogCommentFactory.CreateReply(depth1, content: "deep reply");

        depth2.ParentCommentId.Should().Be(depth1.Id);
    }

    [Fact]
    public void BlogComment_Reply_AtDepthGreaterThanMax_IsRejected()
    {
        var root = TestBlogCommentFactory.CreateRoot();
        var depth1 = TestBlogCommentFactory.CreateReply(root);
        var depth2 = TestBlogCommentFactory.CreateReply(depth1);

        // Attempt depth 3 — must be rejected.
        var act = () => TestBlogCommentFactory.CreateReply(depth2, content: "too deep");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MaxDepthExceeded*");
    }

    [Fact]
    public void BlogComment_Reply_ParentBlogMismatch_IsRejected()
    {
        var rootOnBlogA = TestBlogCommentFactory.CreateRoot(blogId: Guid.NewGuid());
        var otherBlogId = Guid.NewGuid();

        var act = () => BlogComment.Create(
            blogId:     otherBlogId,
            userId:     Guid.NewGuid(),
            content:    "mismatched reply",
            parent:     rootOnBlogA,
            blogStatus: BlogStatus.Published,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ParentBlogMismatch*");
    }

    // ── Reactions ─────────────────────────────────────────────────────────────

    [Fact]
    public void BlogComment_AddReaction_AddsNewReaction_AndRaisesEvent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var userId = Guid.NewGuid();
        comment.ClearDomainEvents();
        var now = DateTime.UtcNow.AddMinutes(1);

        comment.AddOrReplaceReaction(userId, ReactionType.Like, now);

        comment.Reactions.Should().ContainSingle()
            .Which.UserId.Should().Be(userId);
        comment.Reactions.Single().ReactionType.Should().Be(ReactionType.Like);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogCommentReactionChangedDomainEvent>(comment);
        evt.CommentId.Should().Be(comment.Id);
        evt.BlogId.Should().Be(comment.BlogId);
        evt.UserId.Should().Be(userId);
        evt.OldType.Should().BeNull();
        evt.NewType.Should().Be(ReactionType.Like);
        evt.OccurredAtUtc.Should().Be(now);
    }

    [Fact]
    public void BlogComment_AddReaction_ReplacesExistingReaction_FromSameUser()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var userId = Guid.NewGuid();
        comment.AddOrReplaceReaction(userId, ReactionType.Like, DateTime.UtcNow);
        comment.ClearDomainEvents();
        var now = DateTime.UtcNow.AddMinutes(2);

        comment.AddOrReplaceReaction(userId, ReactionType.Insightful, now);

        comment.Reactions.Should().ContainSingle(
            "the aggregate enforces one reaction per user per comment");
        comment.Reactions.Single().ReactionType.Should().Be(ReactionType.Insightful);

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogCommentReactionChangedDomainEvent>(comment);
        evt.OldType.Should().Be(ReactionType.Like);
        evt.NewType.Should().Be(ReactionType.Insightful);
        evt.OccurredAtUtc.Should().Be(now);
    }

    [Fact]
    public void BlogComment_AddReaction_SameReaction_IsIdempotent_NoEvent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var userId = Guid.NewGuid();
        comment.AddOrReplaceReaction(userId, ReactionType.Like, DateTime.UtcNow);
        comment.ClearDomainEvents();

        comment.AddOrReplaceReaction(userId, ReactionType.Like, DateTime.UtcNow.AddMinutes(1));

        comment.Reactions.Should().ContainSingle();
        comment.Reactions.Single().ReactionType.Should().Be(ReactionType.Like);
        comment.DomainEvents.Should().BeEmpty(
            "idempotent same-reaction add must NOT raise another event");
    }

    [Fact]
    public void BlogComment_AddReaction_MultipleUsers_EachKeepsOwnReaction()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        comment.AddOrReplaceReaction(userA, ReactionType.Like, DateTime.UtcNow);
        comment.AddOrReplaceReaction(userB, ReactionType.Helpful, DateTime.UtcNow);

        comment.Reactions.Should().HaveCount(2);
        comment.Reactions.Single(r => r.UserId == userA).ReactionType.Should().Be(ReactionType.Like);
        comment.Reactions.Single(r => r.UserId == userB).ReactionType.Should().Be(ReactionType.Helpful);
    }

    [Fact]
    public void BlogComment_AddReaction_OnRedactedComment_IsRejected()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        comment.Redact(DateTime.UtcNow);

        var act = () => comment.AddOrReplaceReaction(
            Guid.NewGuid(), ReactionType.Like, DateTime.UtcNow.AddMinutes(1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Redacted*");
    }

    [Fact]
    public void BlogComment_AddReaction_RejectsEmptyUserId()
    {
        var comment = TestBlogCommentFactory.CreateRoot();

        var act = () => comment.AddOrReplaceReaction(
            Guid.Empty, ReactionType.Like, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>().WithMessage("*UserId*");
    }

    [Fact]
    public void BlogComment_RemoveReaction_RemovesExistingReaction_AndRaisesEvent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var userId = Guid.NewGuid();
        comment.AddOrReplaceReaction(userId, ReactionType.Helpful, DateTime.UtcNow);
        comment.ClearDomainEvents();
        var now = DateTime.UtcNow.AddMinutes(2);

        var removed = comment.RemoveReaction(userId, now);

        removed.Should().BeTrue();
        comment.Reactions.Should().BeEmpty();

        var evt = DomainEventAssertions.ShouldContainDomainEvent<BlogCommentReactionChangedDomainEvent>(comment);
        evt.OldType.Should().Be(ReactionType.Helpful);
        evt.NewType.Should().BeNull();
        evt.OccurredAtUtc.Should().Be(now);
    }

    [Fact]
    public void BlogComment_RemoveReaction_MissingReaction_ReturnsFalse_NoEvent()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var unknownUser = Guid.NewGuid();
        comment.ClearDomainEvents();

        var removed = comment.RemoveReaction(unknownUser, DateTime.UtcNow);

        removed.Should().BeFalse();
        comment.DomainEvents.Should().BeEmpty(
            "no event should fire when there is no reaction to remove");
    }

    [Fact]
    public void BlogComment_RemoveReaction_OnRedactedComment_IsAllowed()
    {
        // Domain spec: removal works even on redacted comments — only Add is blocked.
        var comment = TestBlogCommentFactory.CreateRoot();
        var userId = Guid.NewGuid();
        comment.AddOrReplaceReaction(userId, ReactionType.Like, DateTime.UtcNow);
        comment.Redact(DateTime.UtcNow.AddMinutes(1));
        comment.ClearDomainEvents();

        var removed = comment.RemoveReaction(userId, DateTime.UtcNow.AddMinutes(2));

        removed.Should().BeTrue();
        comment.Reactions.Should().BeEmpty();
        DomainEventAssertions.ShouldContainDomainEvent<BlogCommentReactionChangedDomainEvent>(comment);
    }

    [Fact]
    public void BlogComment_RemoveReaction_RejectsEmptyUserId()
    {
        var comment = TestBlogCommentFactory.CreateRoot();

        var act = () => comment.RemoveReaction(Guid.Empty, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>().WithMessage("*UserId*");
    }
}
