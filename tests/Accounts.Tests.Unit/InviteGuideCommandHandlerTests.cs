using Accounts.Application.Commands.Agency.InviteGuide;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Tests.Unit;

/// <summary>
/// Handler tests for <see cref="InviteGuideCommandHandler"/> covering the GAP-a
/// existence + approved-state guards added for the target guide and the caller.
///
/// Guard order under test:
///   1. caller is an approved Agency            (Forbidden if not an agency / not approved)
///   2. target guide exists and is IndependentGuide (NotFound)
///   3. target guide is approved                (UnprocessableEntity / 422)
///   4. not already affiliated                  (Conflict)
///   5. no pending invitation                   (Conflict)
/// </summary>
public sealed class InviteGuideCommandHandlerTests
{
    private readonly IProviderApplicationRepository _providerRepo = Substitute.For<IProviderApplicationRepository>();
    private readonly IAgencyInvitationRepository _invitationRepo = Substitute.For<IAgencyInvitationRepository>();
    private readonly IAgencyAffiliationRepository _affiliationRepo = Substitute.For<IAgencyAffiliationRepository>();
    private readonly IAccountsUnitOfWork _uow = Substitute.For<IAccountsUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    private readonly Guid _agencyUserId = Guid.NewGuid();
    private readonly Guid _guideUserId = Guid.NewGuid();

    private InviteGuideCommandHandler CreateSut()
    {
        _currentUser.UserId.Returns(_agencyUserId);
        return new InviteGuideCommandHandler(
            _providerRepo, _invitationRepo, _affiliationRepo,
            _uow, _currentUser, _cache,
            NullLogger<InviteGuideCommandHandler>.Instance);
    }

    private InviteGuideCommand Command() => new(_guideUserId, "Join my agency", 12.5m);

    [Fact]
    public async Task Handle_WhenCallerNotAnAgency_ReturnsForbidden()
    {
        var caller = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Approved);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(caller);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenCallerNotApproved_ReturnsForbidden()
    {
        var caller = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.Agency, ProviderApplicationStatus.Pending);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(caller);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenTargetGuideMissing_ReturnsNotFound()
    {
        var caller = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.Agency, ProviderApplicationStatus.Approved);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(caller);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns((ProviderApplication?)null);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTargetGuideNotApproved_ReturnsUnprocessableEntity()
    {
        var caller = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.Agency, ProviderApplicationStatus.Approved);
        var target = AgencyTestData.ProviderApplication(_guideUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Pending);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(caller);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(target);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
    }

    [Fact]
    public async Task Handle_WhenAllGuardsPass_CreatesInvitationAndReturnsId()
    {
        var caller = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.Agency, ProviderApplicationStatus.Approved);
        var target = AgencyTestData.ProviderApplication(_guideUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Approved);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(caller);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(target);
        _affiliationRepo.IsGuideAffiliatedAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(false);
        _invitationRepo.HasPendingInvitationAsync(_agencyUserId, _guideUserId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _invitationRepo.Received(1).Add(Arg.Is<AgencyInvitation>(i =>
            i.AgencyUserId == _agencyUserId && i.GuideUserId == _guideUserId));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
