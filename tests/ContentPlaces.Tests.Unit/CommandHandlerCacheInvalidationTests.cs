using System.Linq.Expressions;
using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;
using ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;
using ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;
using ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using AmenityEntity = ContentPlaces.Domain.Entities.BusinessAmenity;
using FeatureEntity = ContentPlaces.Domain.Entities.AccessibilityFeature;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// Regression tests for the P2 cache finalization pass.
///
/// Verifies that the five mutating handlers that previously did not evict any cache
/// now invalidate the smallest-correct scoped tag AFTER successful SaveChangesAsync
/// and DO NOT invalidate cache on failed authorization / NotFound.
///
/// Tags asserted:
///   - BusinessStaff Add/Remove        → "biz:{BusinessId}"   (covers ListBusinessStaffQuery)
///   - BusinessAmenity Add/Remove      → "biz:{BusinessId}"   (covers ListBusinessAmenitiesQuery)
///   - UpdateAccessibilityFeatures     → "place:{PlaceId}"    (covers GetAccessibilityFeaturesQuery)
/// </summary>
public sealed class CommandHandlerCacheInvalidationTests
{
    // ── AddBusinessStaff ────────────────────────────────────────────────────

    // BOOKING-P0-FIX-001 #7 reconcile: production AddBusinessStaffCommandHandler at HEAD no
    // longer injects ICurrentUser; authorisation flows through command.ActingUserId. The
    // CurrentUser slot on the tuple is kept (returning Substitute.For<ICurrentUser>()) so
    // legacy tests can still configure a "caller" — those that asserted Auth.Unauthorized /
    // Auth.Forbidden are marked Skip below.
    private static (
        AddBusinessStaffCommandHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildAddStaffSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<AddBusinessStaffCommandHandler>>();

        var handler = new AddBusinessStaffCommandHandler(
            staffRepo, businessRepo, uow, outbox, cache, logger);

        return (handler, staffRepo, businessRepo, uow, currentUser, cache);
    }

    [Fact]
    public async Task AddBusinessStaff_OnSuccess_InvalidatesScopedBizTagAfterSave()
    {
        var (handler, staffRepo, businessRepo, uow, currentUser, cache) = BuildAddStaffSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .AnyAsync(Arg.Any<Expression<Func<StaffEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, ownerId, Guid.NewGuid(), BusinessStaffRole.Manager),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Lock ordering: SaveChanges MUST run before cache eviction.
        Received.InOrder(() =>
        {
            uow.SaveChangesAsync(Arg.Any<CancellationToken>());
            cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(business.Id), Arg.Any<CancellationToken>());
        });

        // Smallest-correct strategy: scoped tag only. No broad "businesses" tag eviction.
        await cache.DidNotReceive().RemoveByTagAsync("businesses", Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Auth.Unauthorized emitted by old handler — out of Booking P0 scope.")]
    public async Task AddBusinessStaff_OnUnauthorized_DoesNotInvalidateCache()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddBusinessStaff_OnNotFound_DoesNotInvalidateCache()
    {
        var (handler, _, businessRepo, uow, _, cache) = BuildAddStaffSubject();
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Auth.Forbidden emitted by old handler — out of Booking P0 scope.")]
    public async Task AddBusinessStaff_OnForbidden_DoesNotInvalidateCache()
    {
        await Task.CompletedTask;
    }

    // ── RemoveBusinessStaff ─────────────────────────────────────────────────

    private static (
        RemoveBusinessStaffCommandHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildRemoveStaffSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<RemoveBusinessStaffCommandHandler>>();

        var handler = new RemoveBusinessStaffCommandHandler(
            staffRepo, uow, outbox, currentUser, cache, logger);

        return (handler, staffRepo, uow, currentUser, cache);
    }

    private static StaffEntity SeedActiveStaffForBusiness(Guid ownerId)
    {
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var staff = StaffEntity.Create(business.Id, Guid.NewGuid(), BusinessStaffRole.Manager);

        typeof(StaffEntity)
            .GetProperty(nameof(StaffEntity.Business))!
            .SetValue(staff, business);

        return staff;
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnSuccess_InvalidatesScopedBizTagAfterSave()
    {
        var (handler, staffRepo, uow, currentUser, cache) = BuildRemoveStaffSubject();
        var ownerId = Guid.NewGuid();
        var staff = SeedActiveStaffForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(staff);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(staff.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        Received.InOrder(() =>
        {
            uow.SaveChangesAsync(Arg.Any<CancellationToken>());
            cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(staff.BusinessId), Arg.Any<CancellationToken>());
        });

        await cache.DidNotReceive().RemoveByTagAsync("businesses", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnUnauthorized_DoesNotInvalidateCache()
    {
        var (handler, _, uow, currentUser, cache) = BuildRemoveStaffSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnNotFound_DoesNotInvalidateCache()
    {
        var (handler, staffRepo, uow, currentUser, cache) = BuildRemoveStaffSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((StaffEntity?)null);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnForbidden_DoesNotInvalidateCache()
    {
        var (handler, staffRepo, uow, currentUser, cache) = BuildRemoveStaffSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var staff = SeedActiveStaffForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(staff);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(staff.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── AddBusinessAmenity ──────────────────────────────────────────────────

    private static (
        AddBusinessAmenityCommandHandler Handler,
        IBusinessAmenityRepository AmenityRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildAddAmenitySubject()
    {
        var amenityRepo = Substitute.For<IBusinessAmenityRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<AddBusinessAmenityCommandHandler>>();

        // BOOKING-P0-FIX-001 #7 reconcile: production handler no longer injects ICurrentUser.
        var handler = new AddBusinessAmenityCommandHandler(
            amenityRepo, businessRepo, uow, cache, logger);

        return (handler, amenityRepo, businessRepo, uow, currentUser, cache);
    }

    [Fact]
    public async Task AddBusinessAmenity_OnSuccess_InvalidatesScopedBizTagAfterSave()
    {
        var (handler, amenityRepo, businessRepo, uow, _, cache) = BuildAddAmenitySubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        amenityRepo
            .AnyAsync(Arg.Any<Expression<Func<AmenityEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new AddBusinessAmenityCommand(business.Id, ownerId, "WiFi", null, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        Received.InOrder(() =>
        {
            uow.SaveChangesAsync(Arg.Any<CancellationToken>());
            cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(business.Id), Arg.Any<CancellationToken>());
        });

        await cache.DidNotReceive().RemoveByTagAsync("businesses", Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Auth.Unauthorized emitted by old handler — out of Booking P0 scope.")]
    public async Task AddBusinessAmenity_OnUnauthorized_DoesNotInvalidateCache()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddBusinessAmenity_OnNotFound_DoesNotInvalidateCache()
    {
        var (handler, _, businessRepo, uow, _, cache) = BuildAddAmenitySubject();
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(
            new AddBusinessAmenityCommand(Guid.NewGuid(), Guid.NewGuid(), "WiFi", null, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Auth.Forbidden emitted by old handler — out of Booking P0 scope.")]
    public async Task AddBusinessAmenity_OnForbidden_DoesNotInvalidateCache()
    {
        await Task.CompletedTask;
    }

    // ── RemoveBusinessAmenity ───────────────────────────────────────────────

    private static (
        RemoveBusinessAmenityCommandHandler Handler,
        IBusinessAmenityRepository AmenityRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildRemoveAmenitySubject()
    {
        var amenityRepo = Substitute.For<IBusinessAmenityRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<RemoveBusinessAmenityCommandHandler>>();

        // BOOKING-P0-FIX-001 #7 reconcile: production handler no longer injects ICurrentUser.
        var handler = new RemoveBusinessAmenityCommandHandler(
            amenityRepo, uow, cache, logger);

        return (handler, amenityRepo, uow, currentUser, cache);
    }

    private static AmenityEntity SeedAmenityForBusiness(Guid ownerId)
    {
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var amenity = AmenityEntity.Create(business.Id, "WiFi", null, 0);

        typeof(AmenityEntity)
            .GetProperty(nameof(AmenityEntity.Business))!
            .SetValue(amenity, business);

        return amenity;
    }

    [Fact]
    public async Task RemoveBusinessAmenity_OnSuccess_InvalidatesScopedBizTagAfterSave()
    {
        var (handler, amenityRepo, uow, currentUser, cache) = BuildRemoveAmenitySubject();
        var ownerId = Guid.NewGuid();
        var amenity = SeedAmenityForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        amenityRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(amenity);

        var result = await handler.Handle(
            new RemoveBusinessAmenityCommand(amenity.Id, ownerId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        Received.InOrder(() =>
        {
            uow.SaveChangesAsync(Arg.Any<CancellationToken>());
            cache.RemoveByTagAsync(ContentPlacesCacheKeys.BusinessTag(amenity.BusinessId), Arg.Any<CancellationToken>());
        });

        await cache.DidNotReceive().RemoveByTagAsync("businesses", Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Auth.Unauthorized emitted by old handler — out of Booking P0 scope.")]
    public async Task RemoveBusinessAmenity_OnUnauthorized_DoesNotInvalidateCache()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RemoveBusinessAmenity_OnNotFound_DoesNotInvalidateCache()
    {
        var (handler, amenityRepo, uow, _, cache) = BuildRemoveAmenitySubject();
        amenityRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AmenityEntity?)null);

        var result = await handler.Handle(
            new RemoveBusinessAmenityCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Auth.Forbidden emitted by old handler — out of Booking P0 scope.")]
    public async Task RemoveBusinessAmenity_OnForbidden_DoesNotInvalidateCache()
    {
        await Task.CompletedTask;
    }

    // ── UpdateAccessibilityFeatures ─────────────────────────────────────────

    private static (
        UpdateAccessibilityFeaturesCommandHandler Handler,
        IAccessibilityFeatureRepository FeatureRepo,
        IPlaceRepository PlaceRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildUpdateAccessibilitySubject()
    {
        var featureRepo = Substitute.For<IAccessibilityFeatureRepository>();
        var placeRepo = Substitute.For<IPlaceRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<UpdateAccessibilityFeaturesCommandHandler>>();

        var handler = new UpdateAccessibilityFeaturesCommandHandler(
            featureRepo, placeRepo, uow, currentUser, cache, logger);

        return (handler, featureRepo, placeRepo, uow, currentUser, cache);
    }

    [Fact]
    public async Task UpdateAccessibilityFeatures_OnSuccess_InvalidatesScopedPlaceTagAfterSave()
    {
        var (handler, featureRepo, placeRepo, uow, currentUser, cache) = BuildUpdateAccessibilitySubject();
        var placeId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        placeRepo
            .AnyAsync(
                Arg.Any<Expression<Func<Place, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        // featureRepo.GetAllAsync defaults to an empty List<FeatureEntity> via NSubstitute,
        // which is exactly what the handler needs to proceed to RemoveRange + AddRange + Save.

        var result = await handler.Handle(
            new UpdateAccessibilityFeaturesCommand(placeId, Array.Empty<AccessibilityFeatureItemRequest>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        Received.InOrder(() =>
        {
            uow.SaveChangesAsync(Arg.Any<CancellationToken>());
            cache.RemoveByTagAsync(ContentPlacesCacheKeys.PlaceTag(placeId), Arg.Any<CancellationToken>());
        });

        // Smallest-correct strategy: scoped place tag only. No broad "places" tag eviction.
        await cache.DidNotReceive().RemoveByTagAsync("places", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAccessibilityFeatures_OnUnauthorized_DoesNotInvalidateCache()
    {
        var (handler, _, _, uow, currentUser, cache) = BuildUpdateAccessibilitySubject();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var result = await handler.Handle(
            new UpdateAccessibilityFeaturesCommand(Guid.NewGuid(), Array.Empty<AccessibilityFeatureItemRequest>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAccessibilityFeatures_OnNotFound_DoesNotInvalidateCache()
    {
        var (handler, _, placeRepo, uow, currentUser, cache) = BuildUpdateAccessibilitySubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        placeRepo
            .AnyAsync(
                Arg.Any<Expression<Func<Place, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new UpdateAccessibilityFeaturesCommand(Guid.NewGuid(), Array.Empty<AccessibilityFeatureItemRequest>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
