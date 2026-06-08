using Accounts.Application.Commands.Agency.ApplyToAgency;
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
/// Handler tests for <see cref="ApplyToAgencyCommandHandler"/> covering the GAP-a
/// existence + approved-state guards added for the target agency and the caller.
///
/// Guard order under test:
///   1. caller is an approved IndependentGuide  (Forbidden if not a guide / not approved)
///   2. target agency exists and is an Agency    (NotFound)
///   3. target agency is approved                (UnprocessableEntity / 422)
///   4. not already affiliated                   (Conflict)
///   5. no pending application                   (Conflict)
/// </summary>
public sealed class ApplyToAgencyCommandHandlerTests
{
    private readonly IProviderApplicationRepository _providerRepo = Substitute.For<IProviderApplicationRepository>();
    private readonly IAgencyApplicationRepository _applicationRepo = Substitute.For<IAgencyApplicationRepository>();
    private readonly IAgencyAffiliationRepository _affiliationRepo = Substitute.For<IAgencyAffiliationRepository>();
    private readonly IAccountsUnitOfWork _uow = Substitute.For<IAccountsUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    private readonly Guid _guideUserId = Guid.NewGuid();
    private readonly Guid _agencyUserId = Guid.NewGuid();

    private ApplyToAgencyCommandHandler CreateSut()
    {
        _currentUser.UserId.Returns(_guideUserId);
        return new ApplyToAgencyCommandHandler(
            _providerRepo, _applicationRepo, _affiliationRepo,
            _uow, _currentUser, _cache,
            NullLogger<ApplyToAgencyCommandHandler>.Instance);
    }

    private ApplyToAgencyCommand Command() => new(_agencyUserId, "Hi, please add me");

    [Fact]
    public async Task Handle_WhenCallerNotApproved_ReturnsForbidden()
    {
        var caller = AgencyTestData.ProviderApplication(_guideUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Pending);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(caller);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenTargetAgencyMissing_ReturnsNotFound()
    {
        var caller = AgencyTestData.ProviderApplication(_guideUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Approved);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(caller);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns((ProviderApplication?)null);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTargetIsNotAnAgencyType_ReturnsNotFound()
    {
        var caller = AgencyTestData.ProviderApplication(_guideUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Approved);
        var target = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.TourOperator, ProviderApplicationStatus.Approved);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(caller);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(target);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTargetAgencyNotApproved_ReturnsUnprocessableEntity()
    {
        var caller = AgencyTestData.ProviderApplication(_guideUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Approved);
        var target = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.Agency, ProviderApplicationStatus.Pending);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(caller);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(target);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
    }

    [Fact]
    public async Task Handle_WhenAllGuardsPass_CreatesApplicationAndReturnsId()
    {
        var caller = AgencyTestData.ProviderApplication(_guideUserId, ProviderType.IndependentGuide, ProviderApplicationStatus.Approved);
        var target = AgencyTestData.ProviderApplication(_agencyUserId, ProviderType.Agency, ProviderApplicationStatus.Approved);
        _providerRepo.GetByUserIdAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(caller);
        _providerRepo.GetByUserIdAsync(_agencyUserId, Arg.Any<CancellationToken>()).Returns(target);
        _affiliationRepo.IsGuideAffiliatedAsync(_guideUserId, Arg.Any<CancellationToken>()).Returns(false);
        _applicationRepo.HasPendingApplicationAsync(_guideUserId, _agencyUserId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _applicationRepo.Received(1).Add(Arg.Is<AgencyApplication>(a =>
            a.GuideUserId == _guideUserId && a.AgencyUserId == _agencyUserId));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
