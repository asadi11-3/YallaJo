using Accounts.Application.Caching;
using Accounts.Application.Queries.Agency.GetMyInvitations;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;

namespace Accounts.Tests.Unit;

/// <summary>
/// Tests for <see cref="GetMyInvitationsQueryHandler"/> covering GAP-b (per-direction
/// status filter) and GAP-d (per-direction cache key).
///
/// - Direction "received" => only Pending invitations for the guide.
/// - Direction "sent"     => all statuses for the agency.
/// - The cache key is scoped by direction so sent/received never collide for a
///   user who is both an agency owner and a guide.
/// </summary>
public sealed class GetMyInvitationsQueryHandlerTests
{
    private readonly IAgencyInvitationRepository _invitationRepo = Substitute.For<IAgencyInvitationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    private readonly Guid _userId = Guid.NewGuid();

    private GetMyInvitationsQueryHandler CreateSut()
    {
        _currentUser.UserId.Returns(_userId);

        // Make the substitute HybridCache actually run the factory delegate so the
        // repository call + filtering is exercised. Capture the key for assertion.
        _cache.GetOrCreateAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, ValueTask<IReadOnlyList<InvitationDto>>>>(),
                Arg.Any<HybridCacheEntryOptions?>(),
                Arg.Any<IEnumerable<string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                _lastCacheKey = callInfo.ArgAt<string>(0);
                var factory = callInfo.ArgAt<Func<CancellationToken, ValueTask<IReadOnlyList<InvitationDto>>>>(1);
                return factory(CancellationToken.None);
            });

        return new GetMyInvitationsQueryHandler(
            _invitationRepo, _currentUser, _cache,
            NullLogger<GetMyInvitationsQueryHandler>.Instance);
    }

    private string _lastCacheKey = string.Empty;

    [Fact]
    public async Task Handle_Received_QueriesGuideSideWithPendingFilter_AndUsesReceivedKey()
    {
        _invitationRepo
            .GetByGuideUserIdAsync(_userId, AgencyInvitationStatus.Pending, Arg.Any<CancellationToken>())
            .Returns(new List<AgencyInvitation>());

        var result = await CreateSut().Handle(new GetMyInvitationsQuery("received"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _invitationRepo.Received(1)
            .GetByGuideUserIdAsync(_userId, AgencyInvitationStatus.Pending, Arg.Any<CancellationToken>());
        await _invitationRepo.DidNotReceive()
            .GetByAgencyUserIdAsync(Arg.Any<Guid>(), Arg.Any<AgencyInvitationStatus?>(), Arg.Any<CancellationToken>());
        _lastCacheKey.Should().Be(AccountsCacheKeys.AgencyInvitations(_userId, "received"));
    }

    [Fact]
    public async Task Handle_Sent_QueriesAgencySideWithNoFilter_AndUsesSentKey()
    {
        _invitationRepo
            .GetByAgencyUserIdAsync(_userId, null, Arg.Any<CancellationToken>())
            .Returns(new List<AgencyInvitation>());

        var result = await CreateSut().Handle(new GetMyInvitationsQuery("sent"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _invitationRepo.Received(1)
            .GetByAgencyUserIdAsync(_userId, null, Arg.Any<CancellationToken>());
        await _invitationRepo.DidNotReceive()
            .GetByGuideUserIdAsync(Arg.Any<Guid>(), Arg.Any<AgencyInvitationStatus?>(), Arg.Any<CancellationToken>());
        _lastCacheKey.Should().Be(AccountsCacheKeys.AgencyInvitations(_userId, "sent"));
    }

    [Fact]
    public void CacheKeys_AreDistinct_PerDirection_ForSameUser()
    {
        var received = AccountsCacheKeys.AgencyInvitations(_userId, "received");
        var sent = AccountsCacheKeys.AgencyInvitations(_userId, "sent");

        received.Should().NotBe(sent, "sent and received must use distinct keys to avoid dual-role collisions");
    }
}
