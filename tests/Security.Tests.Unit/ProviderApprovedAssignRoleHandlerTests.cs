using System.Linq.Expressions;
using Accounts.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Application.Interfaces;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using Security.Infrastructure.EventHandlers;
using Security.Tests.Unit.TestFixtures;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Tests.Unit;

/// <summary>
/// Verifies that approving a provider issues the server-generated <c>provider_id</c>
/// identity claim (value = approved ProviderApplication.Id) in addition to the role,
/// idempotently. This claim is what lets provider-scoped Finance endpoints resolve the
/// caller's provider from the JWT (it must never be trusted from a client).
/// </summary>
public sealed class ProviderApprovedAssignRoleHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserClaimRepository _userClaimRepository = Substitute.For<IUserClaimRepository>();
    private readonly ISecurityUnitOfWork _unitOfWork = Substitute.For<ISecurityUnitOfWork>();
    private readonly ISecurityInboxStore _inboxStore = Substitute.For<ISecurityInboxStore>();

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProviderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private ProviderApprovedAssignRoleHandler Build() =>
        new(_userRepository, _roleRepository, _userClaimRepository, _unitOfWork, _inboxStore,
            NullLogger<ProviderApprovedAssignRoleHandler>.Instance);

    private static IntegrationEventNotification<ProviderApprovedIntegrationEvent> Notification(
        string providerType = "TourOperator") =>
        new(
            MessageId: Guid.NewGuid(),
            Event: new ProviderApprovedIntegrationEvent(
                ApplicationId: ProviderId,
                UserId: UserId,
                ProviderType: providerType,
                ApprovedAt: DateTime.UtcNow,
                ApprovedByAdminId: Guid.NewGuid()));

    private void StubProviderRole()
    {
        var role = Role.Create(AppRoles.Provider);
        _roleRepository
            .GetRolesByNamesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns([role]);
    }

    [Fact]
    public async Task Handle_IssuesProviderIdClaim_WhenProviderApprovedAndClaimAbsent()
    {
        StubProviderRole();
        _inboxStore.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.GetUserRoleAsync(UserId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserRole?)null);
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(TestUserBuilder.CreateUserWithRoles(UserId));
        _userClaimRepository
            .AnyAsync(Arg.Any<Expression<Func<UserClaim, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Build().Handle(Notification(), CancellationToken.None);

        await _userClaimRepository.Received(1).AddAsync(
            Arg.Is<UserClaim>(c =>
                c.UserId == UserId
                && c.ClaimType == ProviderClaimTypes.ProviderId
                && c.ClaimValue == ProviderId.ToString()),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotDuplicateClaim_WhenProviderIdClaimAlreadyExists()
    {
        StubProviderRole();
        _inboxStore.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.GetUserRoleAsync(UserId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserRole?)null);
        _userRepository.GetByIdAsync(UserId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(TestUserBuilder.CreateUserWithRoles(UserId));
        _userClaimRepository
            .AnyAsync(Arg.Any<Expression<Func<UserClaim, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true); // claim already present

        await Build().Handle(Notification(), CancellationToken.None);

        await _userClaimRepository.DidNotReceive().AddAsync(
            Arg.Any<UserClaim>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StillIssuesClaim_WhenRoleAlreadyAssigned()
    {
        StubProviderRole();
        _inboxStore.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        // User already has the role → handler takes the early-return path, but must
        // still backfill the provider_id claim.
        _userRepository.GetUserRoleAsync(UserId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(UserRole.Create(UserId, Guid.NewGuid()));
        _userClaimRepository
            .AnyAsync(Arg.Any<Expression<Func<UserClaim, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Build().Handle(Notification(), CancellationToken.None);

        await _userClaimRepository.Received(1).AddAsync(
            Arg.Is<UserClaim>(c =>
                c.ClaimType == ProviderClaimTypes.ProviderId
                && c.ClaimValue == ProviderId.ToString()),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IsIdempotent_WhenMessageAlreadyProcessed()
    {
        _inboxStore.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        await Build().Handle(Notification(), CancellationToken.None);

        await _userClaimRepository.DidNotReceive().AddAsync(
            Arg.Any<UserClaim>(), Arg.Any<CancellationToken>());
    }
}
