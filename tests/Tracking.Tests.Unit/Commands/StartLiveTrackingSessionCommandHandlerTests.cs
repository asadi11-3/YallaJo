using Booking.Contracts.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tracking.Application.Commands.StartLiveTrackingSession;
using Tracking.Application.Interfaces;
using Tracking.Domain.Entities;
using Tracking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Tracking.Tests.Unit.Commands;

public sealed class StartLiveTrackingSessionCommandHandlerTests
{
    private static readonly Guid UserId        = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourBookingId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TourGuideId   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid GuideUserId   = UserId; // guide user matches command caller

    /// <summary>
    /// Helper: builds an <see cref="EntityOwnershipResolution"/> representing a found,
    /// active entity owned by the given user.
    /// </summary>
    private static EntityOwnershipResolution Found(Guid? ownerUserId)
        => new(IsSupported: true, Exists: true, IsDeleted: false, OwnerUserId: ownerUserId);

    /// <summary>
    /// Helper: builds an <see cref="EntityOwnershipResolution"/> representing a missing entity.
    /// </summary>
    private static EntityOwnershipResolution NotFound()
        => new(IsSupported: true, Exists: false, IsDeleted: false, OwnerUserId: null);

    private static (
        StartLiveTrackingSessionCommandHandler handler,
        ITrackingSessionRepository repo,
        ITrackingUnitOfWork uow,
        ITourGuideOwnershipService ownershipService,
        ICurrentUser currentUser)
        BuildSut(bool isAuthenticated = true, Guid? actorUserId = null)
    {
        var repo             = Substitute.For<ITrackingSessionRepository>();
        var uow              = Substitute.For<ITrackingUnitOfWork>();
        var ownershipService = Substitute.For<ITourGuideOwnershipService>();
        var currentUser      = Substitute.For<ICurrentUser>();
        var logger           = Substitute.For<ILogger<StartLiveTrackingSessionCommandHandler>>();

        currentUser.IsAuthenticated.Returns(isAuthenticated);
        currentUser.UserId.Returns(actorUserId ?? UserId);

        // Default: guide exists, is active, belongs to actorUserId
        ownershipService
            .GetTourGuideOwnershipAsync(TourGuideId, Arg.Any<CancellationToken>())
            .Returns(Found(GuideUserId));

        // Default: no existing active session
        repo.HasActiveSessionForBookingAsync(TourBookingId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new StartLiveTrackingSessionCommandHandler(
            repo, uow, ownershipService, currentUser, logger);

        return (handler, repo, uow, ownershipService, currentUser);
    }

    private static StartLiveTrackingSessionCommand ValidCommand(IReadOnlyList<Guid>? waypoints = null)
        => new(UserId, TourBookingId, TourGuideId, DateTime.UtcNow, waypoints);

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_valid_command_creates_session_and_calls_uow()
    {
        var (handler, repo, uow, _, _) = BuildSut();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.TourBookingId.Should().Be(TourBookingId);
        result.Value.TourGuideId.Should().Be(TourGuideId);
        result.Value.UserId.Should().Be(UserId);

        await repo.Received(1).AddAsync(Arg.Any<LiveTrackingSession>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_with_waypoints_registers_checkpoints_on_session()
    {
        var (handler, repo, _, _, _) = BuildSut();
        var waypoints = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        LiveTrackingSession? captured = null;
        await repo.AddAsync(Arg.Do<LiveTrackingSession>(s => captured = s), Arg.Any<CancellationToken>());

        var result = await handler.Handle(ValidCommand(waypoints), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.TourCheckpoints.Should().HaveCount(2);
    }

    // ── Not authenticated ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_unauthenticated_returns_Unauthorized()
    {
        var (handler, _, _, _, _) = BuildSut(isAuthenticated: false);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }

    // ── Guide not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_when_guide_not_found_returns_NotFound()
    {
        var (handler, _, _, ownershipService, _) = BuildSut();

        ownershipService
            .GetTourGuideOwnershipAsync(TourGuideId, Arg.Any<CancellationToken>())
            .Returns(NotFound());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_when_guide_is_deleted_returns_NotFound()
    {
        var (handler, _, _, ownershipService, _) = BuildSut();

        ownershipService
            .GetTourGuideOwnershipAsync(TourGuideId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(
                IsSupported: true, Exists: true, IsDeleted: true, OwnerUserId: GuideUserId));

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    // ── Guide belongs to different user ───────────────────────────────────────

    [Fact]
    public async Task Handle_when_guide_belongs_to_different_user_returns_Forbidden()
    {
        var differentUser = Guid.NewGuid();
        var (handler, _, _, ownershipService, _) = BuildSut();

        ownershipService
            .GetTourGuideOwnershipAsync(TourGuideId, Arg.Any<CancellationToken>())
            .Returns(Found(differentUser));

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    // ── Duplicate session ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_when_active_session_already_exists_returns_Conflict()
    {
        var repo2            = Substitute.For<ITrackingSessionRepository>();
        var ownershipService = Substitute.For<ITourGuideOwnershipService>();
        var currentUser      = Substitute.For<ICurrentUser>();
        var uow              = Substitute.For<ITrackingUnitOfWork>();
        var logger           = Substitute.For<ILogger<StartLiveTrackingSessionCommandHandler>>();

        repo2.HasActiveSessionForBookingAsync(TourBookingId, Arg.Any<CancellationToken>()).Returns(true);
        ownershipService
            .GetTourGuideOwnershipAsync(TourGuideId, Arg.Any<CancellationToken>())
            .Returns(Found(UserId));
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(UserId);

        var conflictHandler = new StartLiveTrackingSessionCommandHandler(
            repo2, uow, ownershipService, currentUser, logger);

        var result = await conflictHandler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validator_fails_for_empty_UserId()
    {
        var validator = new StartLiveTrackingSessionCommandValidator();
        var cmd = new StartLiveTrackingSessionCommand(Guid.Empty, TourBookingId, TourGuideId, DateTime.UtcNow, null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.UserId));
    }

    [Fact]
    public async Task Validator_fails_for_empty_TourBookingId()
    {
        var validator = new StartLiveTrackingSessionCommandValidator();
        var cmd = new StartLiveTrackingSessionCommand(UserId, Guid.Empty, TourGuideId, DateTime.UtcNow, null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.TourBookingId));
    }

    [Fact]
    public async Task Validator_fails_for_empty_TourGuideId()
    {
        var validator = new StartLiveTrackingSessionCommandValidator();
        var cmd = new StartLiveTrackingSessionCommand(UserId, TourBookingId, Guid.Empty, DateTime.UtcNow, null);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.TourGuideId));
    }

    [Fact]
    public async Task Validator_fails_when_waypoint_list_contains_empty_guid()
    {
        var validator = new StartLiveTrackingSessionCommandValidator();
        var cmd = new StartLiveTrackingSessionCommand(
            UserId, TourBookingId, TourGuideId, DateTime.UtcNow,
            [Guid.NewGuid(), Guid.Empty]);

        var result = await validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
    }
}
