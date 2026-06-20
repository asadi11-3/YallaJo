using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentTours.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-B1 — Development / QA-only seeder that creates two approved tours
/// (<see cref="DevSeedIds.TourWithImageId"/> and <see cref="DevSeedIds.TourWithoutImageId"/>)
/// plus a public <see cref="TourGuide"/> aggregate (<see cref="DevSeedIds.TourGuideId"/>) with a
/// managed avatar URL.
/// <para>
/// One tour is intended to receive a primary image (via the ContentCore DevImagesSeeder) and the
/// other is left image-less to exercise the placeholder-fallback UI. Guarded by
/// <see cref="IHostEnvironment.IsDevelopment"/>; idempotent per-row.
/// </para>
/// </summary>
public sealed class DevToursSeeder(
    ContentToursDbContext dbContext,
    IHostEnvironment hostEnvironment,
    ILogger<DevToursSeeder> logger) : IModuleDbInitializer
{
    public int Order => 163;

    private static readonly Guid LanguageEnglish = new("eeeeeeee-0000-0000-0000-000000000001");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var seededAny = false;

        seededAny |= await EnsureTourGuideAsync(cancellationToken);
        seededAny |= await EnsureTourAsync(
            DevSeedIds.TourWithImageId,
            "Dev Seed Tour With Image",
            "dev-seed-tour-with-image",
            "A development/QA tour that is paired with a seeded primary image for tour-card image testing.",
            isFeatured: true,
            cancellationToken);
        seededAny |= await EnsureTourAsync(
            DevSeedIds.TourWithoutImageId,
            "Dev Seed Tour Without Image",
            "dev-seed-tour-without-image",
            "A development/QA tour that is intentionally left without an image to exercise placeholder fallback.",
            isFeatured: false,
            cancellationToken);

        if (seededAny)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("DEV-SEED-B1: seeded development tours and tour guide aggregate.");
        }
    }

    private async Task<bool> EnsureTourGuideAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.TourGuides
            .IgnoreQueryFilters()
            .AnyAsync(
                g => g.Id == DevSeedIds.TourGuideId
                     || g.Slug == "dev-seed-guide"
                     || g.UserId == DevSeedIds.GuideUserId,
                cancellationToken);

        if (exists)
        {
            return false;
        }

        var guide = CreateEntity<TourGuide>();
        SetProperty(guide, nameof(TourGuide.Id), DevSeedIds.TourGuideId);
        SetProperty(guide, nameof(TourGuide.UserId), DevSeedIds.GuideUserId);
        SetProperty(guide, nameof(TourGuide.Slug), "dev-seed-guide");
        SetProperty(guide, nameof(TourGuide.DisplayName), "Dev Seed Guide");
        SetProperty(guide, nameof(TourGuide.Bio), "Development/QA seeded tour guide used for manual media and profile testing.");
        SetProperty(guide, nameof(TourGuide.YearsOfExperience), 6);
        SetProperty(guide, nameof(TourGuide.HasFirstAid), true);
        SetProperty(guide, nameof(TourGuide.AvatarUrl), "/assets/images/gallery/05.jpg");
        SetProperty(guide, nameof(TourGuide.TrustTier), GuideTrustTier.New);
        SetProperty(guide, nameof(TourGuide.Status), TourGuideStatus.Active);
        SetProperty(guide, nameof(TourGuide.IsDeleted), false);
        SetProperty(guide, nameof(TourGuide.CreatedAt), DateTime.UtcNow);

        dbContext.TourGuides.Add(guide);
        return true;
    }

    private async Task<bool> EnsureTourAsync(
        Guid tourId,
        string name,
        string slug,
        string description,
        bool isFeatured,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Tours
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == tourId, cancellationToken);

        if (exists)
        {
            return false;
        }

        var tour = CreateEntity<Tour>();
        SetProperty(tour, nameof(Tour.Id), tourId);
        SetProperty(tour, nameof(Tour.Name), name);
        SetProperty(tour, nameof(Tour.Slug), slug);
        SetProperty(tour, nameof(Tour.Description), description);
        SetProperty(tour, nameof(Tour.ShortDescription), "Dev/QA seeded tour.");
        SetProperty(tour, nameof(Tour.Difficulty), Difficulty.Easy);
        SetProperty(tour, nameof(Tour.DurationMinutes), 240);
        SetProperty(tour, nameof(Tour.MaxGroupSize), 12);
        SetProperty(tour, nameof(Tour.MinAge), (int?)null);
        SetProperty(tour, nameof(Tour.BasePrice), new Money(55m, "JOD"));
        SetProperty(tour, nameof(Tour.Currency), "JOD");
        SetProperty(tour, nameof(Tour.Location), new Location(30.3285m, 35.4444m));
        SetProperty(tour, nameof(Tour.MeetingPoint), new Location(30.3290m, 35.4450m));
        SetProperty(tour, nameof(Tour.Status), TourStatus.Approved);
        SetProperty(tour, nameof(Tour.IsFeatured), isFeatured);
        SetProperty(tour, nameof(Tour.IsInstantBooking), true);
        SetProperty(tour, nameof(Tour.CancellationPolicyHours), 24);
        SetProperty(tour, nameof(Tour.CreatedByUserId), DevSeedIds.GuideUserId);
        SetProperty(tour, nameof(Tour.PlaceId), DevSeedIds.PlaceId);
        SetProperty(tour, nameof(Tour.IsChildFriendly), true);
        SetProperty(tour, nameof(Tour.IsAccessible), true);
        SetProperty(tour, nameof(Tour.AverageRating), 4.6m);
        SetProperty(tour, nameof(Tour.ReviewCount), 8);
        SetProperty(tour, nameof(Tour.BookingCount), 3);

        dbContext.Tours.Add(tour);

        var schedule = CreateEntity<TourSchedule>();
        SetProperty(schedule, nameof(TourSchedule.TourId), tourId);
        SetProperty(schedule, nameof(TourSchedule.DayOfWeek), (byte)1);
        SetProperty(schedule, nameof(TourSchedule.StartTime), new TimeOnly(8, 0));
        SetProperty(schedule, nameof(TourSchedule.EndTime), new TimeOnly(16, 0));
        SetProperty(schedule, nameof(TourSchedule.IsActive), true);
        dbContext.TourSchedules.Add(schedule);

        var tier = CreateEntity<TourPricingTier>();
        SetProperty(tier, nameof(TourPricingTier.TourId), tourId);
        SetProperty(tier, nameof(TourPricingTier.Name), "Adult");
        SetProperty(tier, nameof(TourPricingTier.Description), "Adult pricing tier (dev seed).");
        SetProperty(tier, nameof(TourPricingTier.Price), new Money(55m, "JOD"));
        SetProperty(tier, nameof(TourPricingTier.ParticipantType), ParticipantType.Adult);
        SetProperty(tier, nameof(TourPricingTier.MinParticipants), 1);
        SetProperty(tier, nameof(TourPricingTier.MaxParticipants), 12);
        SetProperty(tier, nameof(TourPricingTier.IsActive), true);
        dbContext.TourPricingTiers.Add(tier);

        var translation = CreateEntity<TourTranslation>();
        SetProperty(translation, nameof(TourTranslation.TourId), tourId);
        SetProperty(translation, nameof(TourTranslation.LanguageId), LanguageEnglish);
        SetProperty(translation, nameof(TourTranslation.Name), name);
        SetProperty(translation, nameof(TourTranslation.Description), description);
        SetProperty(translation, nameof(TourTranslation.ShortDescription), "Dev/QA seeded tour.");
        SetProperty(translation, nameof(TourTranslation.MeetingPoint), "Main visitor centre entrance.");
        dbContext.TourTranslations.Add(translation);

        var waypoint = CreateEntity<TourWaypoint>();
        SetProperty(waypoint, nameof(TourWaypoint.TourId), tourId);
        SetProperty(waypoint, nameof(TourWaypoint.Name), "Starting Point");
        SetProperty(waypoint, nameof(TourWaypoint.Description), "Tour begins here (dev seed).");
        SetProperty(waypoint, nameof(TourWaypoint.Location), new Location(30.3285m, 35.4444m));
        SetProperty(waypoint, nameof(TourWaypoint.SortOrder), 0);
        SetProperty(waypoint, nameof(TourWaypoint.DurationMinutes), 30);
        SetProperty(waypoint, nameof(TourWaypoint.WaypointType), (byte)0);
        dbContext.TourWaypoints.Add(waypoint);

        var tourGuideLink = CreateEntity<TourTourGuide>();
        SetProperty(tourGuideLink, nameof(TourTourGuide.TourId), tourId);
        SetProperty(tourGuideLink, nameof(TourTourGuide.TourGuideId), DevSeedIds.GuideUserId);
        SetProperty(tourGuideLink, nameof(TourTourGuide.IsPrimary), true);
        dbContext.TourTourGuides.Add(tourGuideLink);

        return true;
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        if (Activator.CreateInstance(typeof(TEntity), nonPublic: true) is not TEntity entity)
        {
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        }

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
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");
        }

        property.SetValue(target, value);
    }
}
