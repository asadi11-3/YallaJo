using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Application.Authorization;
using Security.Application.Commands.CreateRole;
using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

/// <summary>
/// Regression tests for the unsafe <c>roleGuard.Errors[0]</c> conversion
/// previously used in <see cref="CreateRoleCommandHandler"/>. Verifies that
/// a guard failure (a) does not throw when its error list is empty and
/// (b) preserves the failure <see cref="Outcome"/> + <see cref="Error"/>
/// when present.
/// </summary>
public sealed class CreateRoleCommandHandlerTests
{
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly ISecurityUnitOfWork _unitOfWork = Substitute.For<ISecurityUnitOfWork>();
    private readonly IRoleHierarchyService _hierarchy = Substitute.For<IRoleHierarchyService>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    private CreateRoleCommandHandler Build() =>
        new(_roleRepository, _unitOfWork, _hierarchy, _cache);

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenGuardFails_WithEmptyErrors()
    {
        // Construct a failure Result that has NO errors and no messages.
        // The previous implementation indexed Errors[0] and would throw
        // ArgumentOutOfRangeException on this shape.
        var emptyFailure = new Result(
            isSuccess: false,
            outcome: Outcome.Forbidden,
            messages: null,
            errors: null);

        _hierarchy.EnsureCanModifyRoleDefinition(Arg.Any<string>())
            .Returns(emptyFailure);

        var sut = Build();

        Func<Task> act = () => sut.Handle(
            new CreateRoleCommand("CustomRole", "desc"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();

        var result = await sut.Handle(
            new CreateRoleCommand("CustomRole", "desc"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPreserve_OutcomeAndErrors_WhenGuardFails()
    {
        _hierarchy.EnsureCanModifyRoleDefinition(Arg.Any<string>())
            .Returns(Result.Failure(UserErrors.RoleBelowActor, Outcome.Forbidden));

        var sut = Build();

        var result = await sut.Handle(
            new CreateRoleCommand("CustomRole", "desc"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UserErrors.RoleBelowActor.Code);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _roleRepository.DidNotReceive().AddAsync(Arg.Any<Security.Domain.Entities.Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenRoleNameIsProtected()
    {
        _hierarchy.EnsureCanModifyRoleDefinition(Arg.Any<string>())
            .Returns(Result.Success());

        var sut = Build();

        var result = await sut.Handle(
            new CreateRoleCommand(AppRoles.Owner, "desc"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(RoleErrors.Protected.Code);
    }
}
