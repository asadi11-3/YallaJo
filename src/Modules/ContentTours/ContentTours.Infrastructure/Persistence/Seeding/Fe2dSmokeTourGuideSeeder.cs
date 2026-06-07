using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentTours.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only smoke seeder (FE-2D) that provisions a single public
/// <see cref="TourGuide"/> aggregate reachable at <c>/guides/fe2d-smoke-guide</c>.
///
/// Rationale: the public TourGuide detail page resolves a <see cref="TourGuide"/> aggregate by
/// slug and exposes its <c>Id</c> as the accessibility-review <c>TargetId</c>. No such aggregate is
/// seeded by <see cref="ContentToursDbInitializer"/> (which only seeds <c>TourTourGuide</c> join
/// rows), so a TourGuide-targeted accessibility review would otherwise have no reachable page.
///
/// This seeder is idempotent (keyed on the deterministic slug / id) and is only ever invoked from
/// <c>UseDataSeedingAsync</c>, which is gated to Development (or explicit <c>Seeding:Enabled</c>).
/// It uses reflection to bypass the domain factory so it can set a deterministic <c>Id</c> and skip
/// raising domain events — matching the existing seeder style in this module.
/// </summary>
public sealed class Fe2dSmokeTourGuideSeeder(ContentToursDbContext dbContext) : IModuleDbInitializer
{
    /// <summary>Deterministic id so the Social accessibility-review seeder can target this guide.</summary>
    public static readonly Guid GuideId = Guid.Parse("fe2d0000-0000-0000-0000-0000000000a1");

    /// <summary>Reuses the seeded "guide.petra@yallajo.local" user (role TourGuide, has a public profile).</summary>
    public static readonly Guid GuideUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public const string Slug = "fe2d-smoke-guide";

    // Runs after ContentToursDbInitializer (80) and Booking (90) so it never collides with their work.
    public int Order => 95;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.TourGuides
            .IgnoreQueryFilters()
            .AnyAsync(g => g.Id == GuideId || g.Slug == Slug || g.UserId == GuideUserId, cancellationToken);

        if (exists)
            return;

        var guide = CreateEntity<TourGuide>();
        SetProperty(guide, nameof(TourGuide.Id), GuideId);
        SetProperty(guide, nameof(TourGuide.UserId), GuideUserId);
        SetProperty(guide, nameof(TourGuide.Slug), Slug);
        SetProperty(guide, nameof(TourGuide.DisplayName), "FE2D Smoke Guide");
        SetProperty(guide, nameof(TourGuide.Bio),
            "FE2D smoke-test tour guide used to visually verify accessibility review UI states. Development data only.");
        SetProperty(guide, nameof(TourGuide.YearsOfExperience), 5);
        SetProperty(guide, nameof(TourGuide.HasFirstAid), true);
        SetProperty(guide, nameof(TourGuide.TrustTier), GuideTrustTier.New);
        SetProperty(guide, nameof(TourGuide.Status), TourGuideStatus.Active);
        SetProperty(guide, nameof(TourGuide.IsDeleted), false);
        SetProperty(guide, nameof(TourGuide.CreatedAt), DateTime.UtcNow);

        dbContext.TourGuides.Add(guide);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");

        property.SetValue(target, value);
    }
}
