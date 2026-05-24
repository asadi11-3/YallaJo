using Booking.Application.Commands.RefundPolicy.UpsertRefundPolicy;
using Booking.Application.Interfaces;
using Booking.Application.Queries.GetRefundPolicyByTour;
using Booking.Contracts.Authorization;
using Booking.Domain.Repositories;
using ContentTours.Contracts.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands.RefundPolicy;

public sealed class UpsertRefundPolicyCommandHandlerTests
{
    private static readonly Guid TourId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    private static IReadOnlyList<RefundTierDto> ValidTiers => new[]
    {
        new RefundTierDto(72, 100m),
        new RefundTierDto(24, 50m),
        new RefundTierDto(0, 0m),
    };

    private static (UpsertRefundPolicyCommandHandler handler,
        IRefundPolicyRepository repo,
        ITourOwnershipService ownership,
        IBookingUnitOfWork uow,
        HybridCache cache,
        ICurrentUser user) BuildSut()
    {
        var repo = Substitute.For<IRefundPolicyRepository>();
        var ownership = Substitute.For<ITourOwnershipService>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var user = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpsertRefundPolicyCommandHandler>>();

        var handler = new UpsertRefundPolicyCommandHandler(repo, ownership, uow, cache, user, logger);
        return (handler, repo, ownership, uow, cache, user);
    }

    [Fact]
    public async Task Unauthenticated_returns_Unauthorized()
    {
        var (handler, _, _, uow, _, user) = BuildSut();
        user.IsAuthenticated.Returns(false);
        user.UserId.Returns((Guid?)null);

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tour_not_found_returns_NotFound()
    {
        var (handler, _, ownership, uow, _, user) = BuildSut();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(Guid.NewGuid());
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, false, false, null));

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Soft_deleted_tour_returns_NotFound()
    {
        var (handler, _, ownership, _, _, user) = BuildSut();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(Guid.NewGuid());
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, true, Guid.NewGuid()));

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Foreign_owner_without_admin_returns_Forbidden()
    {
        var (handler, _, ownership, _, _, user) = BuildSut();
        var viewerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid(); // different user owns the tour
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(viewerId);
        user.HasPermission(Arg.Any<string>()).Returns(false);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, ownerId));

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "RefundPolicy.OwnerMismatch");
    }

    [Fact]
    public async Task Admin_override_allowed_even_if_not_tour_owner()
    {
        var (handler, repo, ownership, uow, _, user) = BuildSut();
        var viewerId = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(viewerId);
        user.HasPermission($"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}")
            .Returns(true);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, someoneElse));
        repo.GetByTourIdAsync(TourId, Arg.Any<CancellationToken>())
            .Returns((Booking.Domain.Entities.RefundPolicy?)null);

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ownership_lookup_unsupported_returns_ServerError_not_a_silent_allow()
    {
        var (handler, _, ownership, _, _, user) = BuildSut();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(Guid.NewGuid());
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(false, false, false, null));

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);
        result.Errors.Should().ContainSingle(e => e.Code == "RefundPolicy.OwnershipUnsupported");
    }

    [Fact]
    public async Task First_call_inserts_and_returns_Created()
    {
        var (handler, repo, ownership, uow, cache, user) = BuildSut();
        var viewerId = Guid.NewGuid();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(viewerId);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, viewerId));
        repo.GetByTourIdAsync(TourId, Arg.Any<CancellationToken>())
            .Returns((Booking.Domain.Entities.RefundPolicy?)null);

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.TourId.Should().Be(TourId);
        result.Value.Tiers.Should().HaveCount(3);
        await repo.Received(1).AddAsync(Arg.Any<Booking.Domain.Entities.RefundPolicy>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            $"refund-policy:tour:{TourId:D}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Second_call_updates_existing_and_returns_Ok()
    {
        var (handler, repo, ownership, uow, _, user) = BuildSut();
        var viewerId = Guid.NewGuid();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(viewerId);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, viewerId));

        var existing = Booking.Domain.Entities.RefundPolicy.Create(TourId, new[]
        {
            new Booking.Domain.ValueObjects.RefundTier(24, 100m),
        });
        repo.GetByTourIdAsync(TourId, Arg.Any<CancellationToken>()).Returns(existing);
        repo.GetByIdTrackedAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, ValidTiers), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        existing.Tiers.Should().HaveCount(3);
        repo.Received(1).Update(existing);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validation_failure_in_domain_returns_Invalid()
    {
        var (handler, repo, ownership, uow, _, user) = BuildSut();
        var viewerId = Guid.NewGuid();
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(viewerId);
        ownership.GetTourOwnershipAsync(TourId, Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(true, true, false, viewerId));
        repo.GetByTourIdAsync(TourId, Arg.Any<CancellationToken>())
            .Returns((Booking.Domain.Entities.RefundPolicy?)null);

        // duplicate hours — domain throws BusinessRuleViolationException
        var badTiers = new[]
        {
            new RefundTierDto(24, 100m),
            new RefundTierDto(24, 50m),
        };

        var result = await handler.Handle(new UpsertRefundPolicyCommand(TourId, badTiers), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "RefundPolicy.InvalidTiers");
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
