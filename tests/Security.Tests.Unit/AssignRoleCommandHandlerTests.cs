using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Application.Authorization;
using Security.Application.Commands.AssignRole;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using Security.Tests.Unit.TestFixtures;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

/// <summary>
/// Wiring test: guarantees that <see cref="AssignRoleCommandHandler"/> actually
/// invokes <see cref="IRoleHierarchyService"/> on both the role being assigned
/// AND the target user BEFORE any domain mutation / persistence occurs.
/// </summary>
public sealed class AssignRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ISecurityUnitOfWork _unitOfWork = Substitute.For<ISecurityUnitOfWork>();
    private readonly IRoleHierarchyService _hierarchy = Substitute.For<IRoleHierarchyService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    private AssignRoleCommandHandler Build() =>
        new(_roleRepository, _userRepository, _unitOfWork, _hierarchy, _cache);

    private Role StubActiveRole(string roleName)
    {
        var role = Role.Create(roleName);
        _roleRepository
            .GetByIdAsync(role.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(role);
        return role;
    }

    private void AllowAllGuards()
    {
        _hierarchy.EnsureCanManageRole(Arg.Any<string>()).Returns(Result.Success());
        _hierarchy
            .EnsureCanManageUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    [Fact]
    public async Task Handle_DeniesAssignment_WhenRoleGuardFails_AdminAssigningAdmin()
    {
        var role = StubActiveRole(AppRoles.Admin);
        _hierarchy
            .EnsureCanManageRole(AppRoles.Admin)
            .Returns(Result.Failure(UserErrors.RoleBelowActor, Outcome.Forbidden));

        var sut = Build();
        var result = await sut.Handle(
            new AssignRoleCommand(Guid.NewGuid(), role.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);

        // Must not even hit the user-target check or persistence when the role
        // guard already fails — short-circuit guarantees no partial mutation.
        await _hierarchy.DidNotReceive().EnsureCanManageUserAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeniesAssignment_WhenTargetGuardFails_AdminTargetingSuperAdmin()
    {
        var role = StubActiveRole(AppRoles.User);
        _hierarchy.EnsureCanManageRole(AppRoles.User).Returns(Result.Success());
        _hierarchy
            .EnsureCanManageUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(UserErrors.InsufficientPrivilege, Outcome.Forbidden));

        var targetUserId = Guid.NewGuid();
        var targetUser = TestUserBuilder.CreateUserWithRoles(targetUserId, AppRoles.SuperAdmin);
        _userRepository
            .GetByIdAsync(targetUserId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(targetUser);

        var sut = Build();
        var result = await sut.Handle(
            new AssignRoleCommand(targetUserId, role.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllowsAssignment_WhenBothGuardsPass()
    {
        var role = StubActiveRole(AppRoles.User);
        AllowAllGuards();

        var targetUserId = Guid.NewGuid();
        var user = TestUserBuilder.CreateUserWithRoles(targetUserId);
        _userRepository
            .GetByIdAsync(targetUserId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);
        _userRepository
            .GetUserRoleAsync(targetUserId, role.Id, Arg.Any<CancellationToken>())
            .Returns((UserRole?)null);

        var sut = Build();
        var result = await sut.Handle(
            new AssignRoleCommand(targetUserId, role.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsRoleInactive_WithoutInvokingHierarchyChecks()
    {
        // Role existence/active checks come before hierarchy to give a clearer
        // error when the role is simply disabled — verify hierarchy isn't
        // called in that path.
        var role = Role.Create(AppRoles.User);
        role.Deactivate();
        _roleRepository
            .GetByIdAsync(role.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(role);

        var sut = Build();
        var result = await sut.Handle(
            new AssignRoleCommand(Guid.NewGuid(), role.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == RoleErrors.Inactive.Code);
        _hierarchy.DidNotReceive().EnsureCanManageRole(Arg.Any<string>());
    }
}
