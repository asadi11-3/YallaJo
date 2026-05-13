using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Application.Authorization;
using Security.Application.Commands.AddRoleClaim;
using Security.Application.Commands.AddUserClaim;
using Security.Application.Commands.RemoveRoleClaim;
using Security.Application.Commands.RemoveUserClaim;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

/// <summary>
/// Locks in the centralized RoleClaim / UserClaim error catalogs. Each
/// handler must surface the catalog's <see cref="Error.Code"/> rather than
/// inline literals so consumers can switch on a stable error code.
/// </summary>
public sealed class ClaimErrorCodesTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleClaimRepository _roleClaimRepository = Substitute.For<IRoleClaimRepository>();
    private readonly IUserClaimRepository _userClaimRepository = Substitute.For<IUserClaimRepository>();
    private readonly ISecurityUnitOfWork _unitOfWork = Substitute.For<ISecurityUnitOfWork>();
    private readonly IRoleHierarchyService _hierarchy = Substitute.For<IRoleHierarchyService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    private void AllowAllRoleHierarchyChecks()
    {
        _hierarchy.EnsureCanModifyRoleDefinition(Arg.Any<string>()).Returns(Result.Success());
        _hierarchy
            .EnsureCanManageUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    // ── AddRoleClaim ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRoleClaim_ShouldReturnRoleClaimDuplicate_WhenClaimAlreadyExists()
    {
        AllowAllRoleHierarchyChecks();

        var role = Role.Create("CustomRole");
        _roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(role);
        _roleClaimRepository
            .AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<RoleClaim, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new AddRoleClaimCommandHandler(
            _roleRepository, _roleClaimRepository, _unitOfWork, _hierarchy, _cache);

        var result = await sut.Handle(
            new AddRoleClaimCommand(role.Id, "permission", "x"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(RoleClaimErrors.Duplicate.Code);
    }

    // ── RemoveRoleClaim ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveRoleClaim_ShouldReturnRoleClaimNotFound_WhenClaimMissing()
    {
        AllowAllRoleHierarchyChecks();

        var role = Role.Create("CustomRole");
        _roleRepository.GetByIdAsync(role.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(role);
        _roleClaimRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((RoleClaim?)null);

        var sut = new RemoveRoleClaimCommandHandler(
            _roleRepository, _roleClaimRepository, _unitOfWork, _hierarchy, _cache);

        var result = await sut.Handle(
            new RemoveRoleClaimCommand(role.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(RoleClaimErrors.NotFound.Code);
    }

    // ── AddUserClaim ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddUserClaim_ShouldReturnUserClaimDuplicate_WhenClaimAlreadyExists()
    {
        AllowAllRoleHierarchyChecks();

        var userId = Guid.NewGuid();
        var user = User.Register("user@example.com", "Jo", "Doe");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);
        _userClaimRepository
            .AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserClaim, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = new AddUserClaimCommandHandler(
            _userRepository, _userClaimRepository, _unitOfWork, _hierarchy, _cache);

        var result = await sut.Handle(
            new AddUserClaimCommand(userId, "permission", "x"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UserClaimErrors.Duplicate.Code);
    }

    // ── RemoveUserClaim ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveUserClaim_ShouldReturnUserClaimNotFound_WhenClaimMissing()
    {
        AllowAllRoleHierarchyChecks();

        var userId = Guid.NewGuid();
        var user = User.Register("user@example.com", "Jo", "Doe");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);
        _userClaimRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((UserClaim?)null);

        var sut = new RemoveUserClaimCommandHandler(
            _userRepository, _userClaimRepository, _unitOfWork, _hierarchy, _cache);

        var result = await sut.Handle(
            new RemoveUserClaimCommand(userId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UserClaimErrors.NotFound.Code);
    }
}
