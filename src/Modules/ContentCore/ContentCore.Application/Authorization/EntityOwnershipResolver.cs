using Booking.Contracts.Authorization;
using ContentBlogs.Contracts.Authorization;
using ContentCore.Domain.Enums;
using ContentPlaces.Contracts.Places;
using ContentTours.Contracts.Authorization;
using Social.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentCore.Application.Authorization;

/// <summary>
/// Default <see cref="IEntityOwnershipResolver"/>. Thin fan-out by
/// <see cref="EntityType"/> to the per-module ownership services. Holds no
/// state, performs no I/O of its own, and never references EF or a DbContext.
/// </summary>
public sealed class EntityOwnershipResolver(
    IPlaceOwnershipService places,
    ITourOwnershipService tours,
    IBlogOwnershipService blogs,
    IReviewOwnershipService reviews,
    ITourGuideOwnershipService tourGuides) : IEntityOwnershipResolver
{
    public Task<EntityOwnershipResolution> ResolveAsync(
        EntityType entityType,
        Guid entityId,
        CancellationToken ct = default) =>
        entityType switch
        {
            EntityType.Place     => places.GetPlaceOwnershipAsync(entityId, ct),
            EntityType.Business  => places.GetBusinessOwnershipAsync(entityId, ct),
            EntityType.Tour      => tours.GetTourOwnershipAsync(entityId, ct),
            EntityType.Blog      => blogs.GetBlogOwnershipAsync(entityId, ct),
            EntityType.Review    => reviews.GetReviewOwnershipAsync(entityId, ct),
            EntityType.TourGuide => tourGuides.GetTourGuideOwnershipAsync(entityId, ct),
            _ => Task.FromResult(new EntityOwnershipResolution(
                IsSupported: false,
                Exists: false,
                IsDeleted: false,
                OwnerUserId: null)),
        };
}
