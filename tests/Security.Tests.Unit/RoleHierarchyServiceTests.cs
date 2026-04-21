using FluentAssertions;
using NSubstitute;
using Security.Application.Authorization;
using Security.Contracts.Authorization;
using Security.Domain.Repositories;
using Security.Tests.Unit.TestFixtures;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

public sealed class RoleHierarchyServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private RoleHierarchyService Build(Guid actingUserId, params string[] actingRoles)
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(actingUserId);
        _currentUser.Roles.Returns(actingRoles);
        return new RoleHierarchyService(_currentUser, _userRepository);
    }

    private void StubTargetUser(Guid targetUserId, params string[] roles)
    {
        var user = TestUserBuilder.CreateUserWithRoles(targetUserId, roles);
        _userRepository
            .GetByIdWithDetailsAsync(targetUserId, Arg.Any<CancellationToken>())
            .Returns(user);
    }

    // ── EnsureCanManageUserAsync ──────────────────────────────────────────────

    [Theory]
    [InlineData(AppRoles.Owner,      AppRoles.SuperAdmin, true)]
    [InlineData(AppRoles.Owner,      AppRoles.Admin,      true)]
    [InlineData(AppRoles.Owner,      AppRoles.User,       true)]
    [InlineData(AppRoles.Owner,      AppRoles.TourGuide,  true)]
    [InlineData(AppRoles.Owner,      AppRoles.Guest,      true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Owner,      false)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.SuperAdmin, false)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Admin,      true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.User,       true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.TourGuide,  true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Guest,      true)]
    [InlineData(AppRoles.Admin,      AppRoles.Owner,      false)]
    [InlineData(AppRoles.Admin,      AppRoles.SuperAdmin, false)]
    [InlineData(AppRoles.Admin,      AppRoles.Admin,      false)]
    [InlineData(AppRoles.Admin,      AppRoles.User,       true)]
    [InlineData(AppRoles.Admin,      AppRoles.TourGuide,  true)]
    [InlineData(AppRoles.Admin,      AppRoles.Guest,      true)]
    [InlineData(AppRoles.User,       AppRoles.User,       false)]
    [InlineData(AppRoles.User,       AppRoles.Admin,      false)]
    [InlineData(AppRoles.TourGuide,  AppRoles.Admin,      false)]
    [InlineData(AppRoles.Guest,      AppRoles.Admin,      false)]
    public async Task EnsureCanManageUserAsync_EnforcesHierarchy(
        string actorRole, string targetRole, bool shouldAllow)
    {
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        StubTargetUser(target, targetRole);

        var sut = Build(actor, actorRole);
        var result = await sut.EnsureCanManageUserAsync(target, CancellationToken.None);

        result.IsSuccess.Should().Be(shouldAllow,
            $"{actorRole} managing user with {targetRole} should be {(shouldAllow ? "allowed" : "denied")}");
        if (!shouldAllow)
        {
            result.Outcome.Should().BeOneOf(Outcome.Forbidden, Outcome.Unauthorized);
        }
    }

    [Fact]
    public async Task EnsureCanManageUserAsync_DeniesSelfManagement_ForPrivilegedActor()
    {
        // Owner trying to modify Owner-self — forbidden at the self check
        // BEFORE the target-level check (because self-modification of one's own
        // privileged roles should always be an admin-flow violation).
        var selfId = Guid.NewGuid();
        StubTargetUser(selfId, AppRoles.Owner);

        var sut = Build(selfId, AppRoles.Owner);
        var result = await sut.EnsureCanManageUserAsync(selfId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task EnsureCanManageUserAsync_DeniesUnauthenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);
        _currentUser.Roles.Returns(Array.Empty<string>());

        var sut = new RoleHierarchyService(_currentUser, _userRepository);
        var result = await sut.EnsureCanManageUserAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }

    [Fact]
    public async Task EnsureCanManageUserAsync_UsesHighestPrivilege_WhenActorHasMultipleRoles()
    {
        // Actor has both Admin and User. Effective level = Admin.
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        StubTargetUser(target, AppRoles.TourGuide);

        var sut = Build(actor, AppRoles.User, AppRoles.Admin, AppRoles.TourGuide);
        var result = await sut.EnsureCanManageUserAsync(target, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCanManageUserAsync_UsesHighestPrivilege_WhenTargetHasMultipleRoles()
    {
        // Target has both Admin AND User. Effective level = Admin. Actor is
        // also Admin → same-level, denied.
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        StubTargetUser(target, AppRoles.User, AppRoles.Admin);

        var sut = Build(actor, AppRoles.Admin);
        var result = await sut.EnsureCanManageUserAsync(target, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task EnsureCanManageUserAsync_AllowsSuperAdmin_OverTargetWithAdminAndUser()
    {
        var actor = Guid.NewGuid();
        var target = Guid.NewGuid();
        StubTargetUser(target, AppRoles.User, AppRoles.Admin);

        var sut = Build(actor, AppRoles.SuperAdmin);
        var result = await sut.EnsureCanManageUserAsync(target, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── EnsureCanManageRole (assign/remove-role-from-user paths) ──────────────

    [Theory]
    [InlineData(AppRoles.Owner,      AppRoles.SuperAdmin, true)]
    [InlineData(AppRoles.Owner,      AppRoles.Admin,      true)]
    [InlineData(AppRoles.Owner,      AppRoles.User,       true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Owner,      false)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.SuperAdmin, false)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Admin,      true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.User,       true)]
    [InlineData(AppRoles.Admin,      AppRoles.SuperAdmin, false)]
    [InlineData(AppRoles.Admin,      AppRoles.Admin,      false)]
    [InlineData(AppRoles.Admin,      AppRoles.User,       true)]
    [InlineData(AppRoles.User,       AppRoles.User,       false)]
    [InlineData(AppRoles.Guest,      AppRoles.User,       false)]
    public void EnsureCanManageRole_EnforcesRoleLevelHierarchy(
        string actorRole, string roleName, bool shouldAllow)
    {
        var sut = Build(Guid.NewGuid(), actorRole);
        var result = sut.EnsureCanManageRole(roleName);

        result.IsSuccess.Should().Be(shouldAllow);
    }

    // ── EnsureCanModifyRoleDefinition (role entity paths) ─────────────────────

    [Theory]
    [InlineData(AppRoles.Owner,      AppRoles.SuperAdmin, true)]
    [InlineData(AppRoles.Owner,      AppRoles.Admin,      true)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.SuperAdmin, false)]
    [InlineData(AppRoles.SuperAdmin, AppRoles.Admin,      true)]
    [InlineData(AppRoles.Admin,      AppRoles.Admin,      false)]
    [InlineData(AppRoles.Admin,      "custom-tenant-role", true)]
    [InlineData(AppRoles.User,       "custom-tenant-role", false)]
    public void EnsureCanModifyRoleDefinition_EnforcesRoleLevelHierarchy(
        string actorRole, string roleName, bool shouldAllow)
    {
        var sut = Build(Guid.NewGuid(), actorRole);
        var result = sut.EnsureCanModifyRoleDefinition(roleName);

        result.IsSuccess.Should().Be(shouldAllow);
    }
}
