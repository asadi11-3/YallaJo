using ContentBlogs.Application.Authorization;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Tests.Unit.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Unit tests for <see cref="BlogCommentAuthorizationGuard"/>.
///
/// <para>
/// The guard implements two distinct policies on top of endpoint-level
/// permission protection:
/// </para>
/// <list type="number">
///   <item>
///     <b>Edit</b>: owner may edit within a 30-minute window; moderators
///     (hierarchy guard accepts) may edit at any time.
///   </item>
///   <item>
///     <b>Delete</b>: owner may delete at any time (no time window);
///     moderators (hierarchy guard accepts) may delete at any time.
///   </item>
/// </list>
///
/// <para>
/// Both paths are IDOR-safe — ownership is derived from the loaded comment,
/// never from the request body.
/// </para>
/// </summary>
public sealed class BlogCommentAuthorizationGuardTests
{
    private static readonly TimeSpan EditWindow = BlogCommentAuthorizationGuard.OwnerEditWindow;

    // ── ResolveEdit ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveEditAuthorization_ReturnsUnauthorized_WhenCurrentUserMissing()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        user.UserId.Returns((Guid?)null);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        var guard = NewGuard(user, hierarchy);

        var result = await guard.ResolveEditAuthorizationAsync(comment, DateTime.UtcNow);

        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors[0].Code.Should().Be("BlogComment.Unauthorized");

        // No hierarchy lookup should happen for an unauthenticated caller.
        await hierarchy.DidNotReceiveWithAnyArgs()
            .EnsureCanManageBlogOwnedByAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveEditAuthorization_AllowsOwner_WithinEditWindow()
    {
        var ownerId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var comment = TestBlogCommentFactory.CreateRoot(userId: ownerId, utcNow: createdAt);

        // 29 minutes after creation — within the 30-minute window.
        var now = createdAt.AddMinutes(29);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        var guard = NewGuard(AuthenticatedAs(ownerId), hierarchy);

        var result = await guard.ResolveEditAuthorizationAsync(comment, now);

        result.IsSuccess.Should().BeTrue();
        // Owner path must NOT consult the hierarchy guard.
        await hierarchy.DidNotReceiveWithAnyArgs()
            .EnsureCanManageBlogOwnedByAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveEditAuthorization_ForbidsOwner_AfterEditWindow()
    {
        var ownerId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var comment = TestBlogCommentFactory.CreateRoot(userId: ownerId, utcNow: createdAt);

        // 31 minutes after creation — outside the 30-minute window.
        var now = createdAt + EditWindow + TimeSpan.FromMinutes(1);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        var guard = NewGuard(AuthenticatedAs(ownerId), hierarchy);

        var result = await guard.ResolveEditAuthorizationAsync(comment, now);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("BlogComment.EditWindowExpired");
        // Owner is short-circuited regardless of window — no hierarchy call.
        await hierarchy.DidNotReceiveWithAnyArgs()
            .EnsureCanManageBlogOwnedByAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveEditAuthorization_AllowsModerator_ForNonOwner_AnyTime()
    {
        // Moderator path: actor != owner; hierarchy guard accepts; window does
        // NOT apply.
        var ownerId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var comment = TestBlogCommentFactory.CreateRoot(userId: ownerId, utcNow: createdAt);

        // Far outside the owner-edit window.
        var now = createdAt + EditWindow + TimeSpan.FromDays(7);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        hierarchy.EnsureCanManageBlogOwnedByAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var guard = NewGuard(AuthenticatedAs(moderatorId), hierarchy);

        var result = await guard.ResolveEditAuthorizationAsync(comment, now);

        result.IsSuccess.Should().BeTrue();
        // Hierarchy MUST have been consulted with the loaded comment's owner id,
        // NOT some request-supplied value (IDOR-safe).
        await hierarchy.Received(1)
            .EnsureCanManageBlogOwnedByAsync(ownerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveEditAuthorization_ForbidsNonOwner_WhenHierarchyGuardFails()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var comment = TestBlogCommentFactory.CreateRoot(userId: ownerId);

        var hierarchy = ForbiddenHierarchy();
        var guard = NewGuard(AuthenticatedAs(actorId), hierarchy);

        var result = await guard.ResolveEditAuthorizationAsync(comment, DateTime.UtcNow);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
    }

    // ── ResolveDelete ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveDeleteAuthorization_ReturnsUnauthorized_WhenCurrentUserMissing()
    {
        var comment = TestBlogCommentFactory.CreateRoot();
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        user.UserId.Returns((Guid?)null);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        var guard = NewGuard(user, hierarchy);

        var result = await guard.ResolveDeleteAuthorizationAsync(comment);

        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors[0].Code.Should().Be("BlogComment.Unauthorized");

        await hierarchy.DidNotReceiveWithAnyArgs()
            .EnsureCanManageBlogOwnedByAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveDeleteAuthorization_AllowsOwner_EvenAfterEditWindow()
    {
        // Unlike Edit, Delete has NO time window for the owner.
        var ownerId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var comment = TestBlogCommentFactory.CreateRoot(userId: ownerId, utcNow: createdAt);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        var guard = NewGuard(AuthenticatedAs(ownerId), hierarchy);

        // 30 days later — still allowed because Delete has no owner window.
        var result = await guard.ResolveDeleteAuthorizationAsync(comment);

        result.IsSuccess.Should().BeTrue();
        await hierarchy.DidNotReceiveWithAnyArgs()
            .EnsureCanManageBlogOwnedByAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveDeleteAuthorization_AllowsModerator_ForNonOwner()
    {
        var ownerId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var comment = TestBlogCommentFactory.CreateRoot(userId: ownerId);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        hierarchy.EnsureCanManageBlogOwnedByAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var guard = NewGuard(AuthenticatedAs(moderatorId), hierarchy);

        var result = await guard.ResolveDeleteAuthorizationAsync(comment);

        result.IsSuccess.Should().BeTrue();
        await hierarchy.Received(1)
            .EnsureCanManageBlogOwnedByAsync(ownerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveDeleteAuthorization_ForbidsNonOwner_WhenHierarchyGuardFails()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var comment = TestBlogCommentFactory.CreateRoot(userId: ownerId);

        var hierarchy = ForbiddenHierarchy();
        var guard = NewGuard(AuthenticatedAs(actorId), hierarchy);

        var result = await guard.ResolveDeleteAuthorizationAsync(comment);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
    }

    // ── IDOR-safety: ownership is derived from comment.UserId, NOT request ────

    [Fact]
    public async Task AuthorizationGuard_DerivesOwnerIdFromLoadedComment()
    {
        // The hierarchy guard MUST be called with the loaded comment's UserId,
        // never a request-supplied value.
        var loadedOwnerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var comment = TestBlogCommentFactory.CreateRoot(userId: loadedOwnerId);

        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        hierarchy.EnsureCanManageBlogOwnedByAsync(loadedOwnerId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var guard = NewGuard(AuthenticatedAs(actorId), hierarchy);
        await guard.ResolveEditAuthorizationAsync(comment, DateTime.UtcNow);
        await guard.ResolveDeleteAuthorizationAsync(comment);

        // Verify hierarchy was queried specifically with the loaded comment's
        // UserId on BOTH paths — not Guid.Empty or anything else.
        await hierarchy.Received(2)
            .EnsureCanManageBlogOwnedByAsync(loadedOwnerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveEdit_RejectsNullComment()
    {
        var guard = NewGuard(AuthenticatedAs(Guid.NewGuid()), Substitute.For<IBlogAuthorHierarchyGuard>());

        Func<Task> act = () =>
            guard.ResolveEditAuthorizationAsync(null!, DateTime.UtcNow);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ResolveDelete_RejectsNullComment()
    {
        var guard = NewGuard(AuthenticatedAs(Guid.NewGuid()), Substitute.For<IBlogAuthorHierarchyGuard>());

        Func<Task> act = () => guard.ResolveDeleteAuthorizationAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ICurrentUser AuthenticatedAs(Guid userId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns<Guid?>(userId);
        return user;
    }

    private static IBlogAuthorHierarchyGuard ForbiddenHierarchy()
    {
        var hierarchy = Substitute.For<IBlogAuthorHierarchyGuard>();
        hierarchy.EnsureCanManageBlogOwnedByAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(
                new Error(
                    "Blog.AuthorHierarchyForbidden",
                    "You cannot manage content created by a user at the same or higher privilege level."),
                Outcome.Forbidden));
        return hierarchy;
    }

    private static BlogCommentAuthorizationGuard NewGuard(
        ICurrentUser user,
        IBlogAuthorHierarchyGuard hierarchy) =>
        new(
            currentUser:          user,
            authorHierarchyGuard: hierarchy,
            logger:               NullLogger<BlogCommentAuthorizationGuard>.Instance);
}
