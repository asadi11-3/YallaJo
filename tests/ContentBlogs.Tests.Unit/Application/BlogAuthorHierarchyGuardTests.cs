using ContentBlogs.Application.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Unit tests for <see cref="BlogAuthorHierarchyGuard"/>.
///
/// <para>
/// Pins the business rule that endpoint-permission protection (Blog/Update,
/// Blog/Delete, Blog/Approve, Blog/Read) is necessary but NOT sufficient: a
/// user can also only manage another user's Blog when their role privilege
/// level is strictly higher than the author's.
/// </para>
///
/// <para>
/// Concrete examples this suite locks down:
/// </para>
/// <list type="bullet">
///   <item>Owner CAN manage SuperAdmin / Admin / Standard authors.</item>
///   <item>SuperAdmin CAN manage Admin authors; CANNOT manage another SuperAdmin or Owner.</item>
///   <item>Admin CAN manage Standard authors; CANNOT manage another Admin or higher.</item>
///   <item>Standard CANNOT manage any other user's blog.</item>
///   <item>Self-management is ALWAYS allowed (the actor and the author are the same user).</item>
/// </list>
/// </summary>
public sealed class BlogAuthorHierarchyGuardTests
{
    [Fact]
    public async Task BlogAuthorHierarchyGuard_ReturnsUnauthorized_WhenUserMissing()
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);
        user.UserId.Returns((Guid?)null);

        var lookup = Substitute.For<IUserPrivilegeLevelReader>();

        var guard = NewGuard(user, lookup);
        var result = await guard.EnsureCanManageBlogOwnedByAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors[0].Code.Should().Be("Blog.Unauthorized");

        await lookup.DidNotReceiveWithAnyArgs()
            .GetPrivilegeLevelAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlogAuthorHierarchyGuard_AllowsSelfManagement()
    {
        // Even a Standard-tier author is allowed to manage their OWN blog
        // because the endpoint MustHavePermissionAttribute has already verified
        // they hold Blog/Update etc. for self-authored content.  This is the key
        // reason we do NOT delegate to IRoleHierarchyService.EnsureCanManageUserAsync
        // (which forbids self-management of user accounts).
        var userId = Guid.NewGuid();
        var user = AuthenticatedAs(userId, AppRoles.User);

        var lookup = Substitute.For<IUserPrivilegeLevelReader>();

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(userId);

        result.IsSuccess.Should().BeTrue();

        // Self-path must not require a privilege-lookup roundtrip.
        await lookup.DidNotReceiveWithAnyArgs()
            .GetPrivilegeLevelAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlogAuthorHierarchyGuard_ForbidsStandardManagingOthers()
    {
        var actor = Guid.NewGuid();
        var author = Guid.NewGuid();
        var user = AuthenticatedAs(actor, AppRoles.User);
        var lookup = Substitute.For<IUserPrivilegeLevelReader>();

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(author);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");

        // Standard tier is short-circuited before the lookup is consulted —
        // never call the DB for a clearly forbidden actor.
        await lookup.DidNotReceiveWithAnyArgs()
            .GetPrivilegeLevelAsync(default, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlogAuthorHierarchyGuard_AllowsOwnerManagingSuperAdmin()
    {
        var owner = Guid.NewGuid();
        var author = Guid.NewGuid();
        var user = AuthenticatedAs(owner, AppRoles.Owner);
        var lookup = LookupReturning(author, RolePrivilegeLevel.SuperAdmin);

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(author);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BlogAuthorHierarchyGuard_AllowsSuperAdminManagingAdmin()
    {
        var actor = Guid.NewGuid();
        var author = Guid.NewGuid();
        var user = AuthenticatedAs(actor, AppRoles.SuperAdmin);
        var lookup = LookupReturning(author, RolePrivilegeLevel.Admin);

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(author);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BlogAuthorHierarchyGuard_ForbidsSuperAdminManagingSuperAdmin()
    {
        var actor = Guid.NewGuid();
        var author = Guid.NewGuid();
        var user = AuthenticatedAs(actor, AppRoles.SuperAdmin);
        var lookup = LookupReturning(author, RolePrivilegeLevel.SuperAdmin);

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(author);

        result.Outcome.Should().Be(Outcome.Forbidden,
            "actors must strictly outrank the author — same tier is forbidden");
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
    }

    [Fact]
    public async Task BlogAuthorHierarchyGuard_ForbidsAdminManagingAdmin()
    {
        var actor = Guid.NewGuid();
        var author = Guid.NewGuid();
        var user = AuthenticatedAs(actor, AppRoles.Admin);
        var lookup = LookupReturning(author, RolePrivilegeLevel.Admin);

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(author);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
    }

    [Fact]
    public async Task BlogAuthorHierarchyGuard_ForbidsAdminManagingSuperAdmin()
    {
        var actor = Guid.NewGuid();
        var author = Guid.NewGuid();
        var user = AuthenticatedAs(actor, AppRoles.Admin);
        var lookup = LookupReturning(author, RolePrivilegeLevel.SuperAdmin);

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(author);

        result.Outcome.Should().Be(Outcome.Forbidden,
            "actor's privilege level is below the author's");
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
    }

    // Bonus: explicit positive proof that the highest tier can manage anyone below.
    [Fact]
    public async Task BlogAuthorHierarchyGuard_AllowsOwnerManagingStandard()
    {
        var owner = Guid.NewGuid();
        var author = Guid.NewGuid();
        var user = AuthenticatedAs(owner, AppRoles.Owner);
        var lookup = LookupReturning(author, RolePrivilegeLevel.Standard);

        var result = await NewGuard(user, lookup)
            .EnsureCanManageBlogOwnedByAsync(author);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ICurrentUser AuthenticatedAs(Guid userId, params string[] roles)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns<Guid?>(userId);
        user.Roles.Returns(roles);
        return user;
    }

    private static IUserPrivilegeLevelReader LookupReturning(
        Guid authorId,
        RolePrivilegeLevel level)
    {
        var lookup = Substitute.For<IUserPrivilegeLevelReader>();
        lookup.GetPrivilegeLevelAsync(authorId, Arg.Any<CancellationToken>())
            .Returns(level);
        return lookup;
    }

    private static BlogAuthorHierarchyGuard NewGuard(
        ICurrentUser user,
        IUserPrivilegeLevelReader lookup) =>
        new(
            currentUser:          user,
            privilegeLevelReader: lookup,
            logger:               NullLogger<BlogAuthorHierarchyGuard>.Instance);
}
