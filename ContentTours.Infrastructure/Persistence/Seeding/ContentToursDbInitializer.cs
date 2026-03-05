using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentTours.Infrastructure.Persistence.Seeding;

public sealed class ContentToursDbInitializer(ContentToursDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid GuideUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid LanguageEnglish = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid LanguageArabic = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002");

    public int Order => 80;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Tours.AnyAsync(cancellationToken))
        {
            return;
        }

        var tour = CreateTour();
        var translations = CreateTranslations();
        var schedules = CreateSchedules();
        var waypoints = CreateWaypoints();
        var pricingTiers = CreatePricingTiers();
        var packages = CreatePackages();
        var packageInclusions = CreatePackageInclusions(packages);
        var tourGuides = CreateTourGuides();

        dbContext.Tours.Add(tour);
        dbContext.TourTranslations.AddRange(translations);
        dbContext.TourSchedules.AddRange(schedules);
        dbContext.TourWaypoints.AddRange(waypoints);
        dbContext.TourPricingTiers.AddRange(pricingTiers);
        dbContext.TourPackages.AddRange(packages);
        dbContext.TourPackageInclusions.AddRange(packageInclusions);
        dbContext.TourTourGuides.AddRange(tourGuides);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Tour CreateTour()
    {
        var tour = CreateEntity<Tour>();
        SetProperty(tour, nameof(Tour.Id), SeedContentIds.TourPetraExplorer);
        SetProperty(tour, nameof(Tour.Name), "Petra Full Day Explorer");
        SetProperty(tour, nameof(Tour.Slug), "petra-full-day-explorer");
        SetProperty(tour, nameof(Tour.Description), "A full-day guided journey through Petra's iconic trails and hidden viewpoints.");
        SetProperty(tour, nameof(Tour.ShortDescription), "Guided Petra day tour with transport and local insights.");
        SetProperty(tour, nameof(Tour.Difficulty), Difficulty.Moderate);
        SetProperty(tour, nameof(Tour.DurationMinutes), 480);
        SetProperty(tour, nameof(Tour.MaxGroupSize), 18);
        SetProperty(tour, nameof(Tour.MinAge), 12);
        SetProperty(tour, nameof(Tour.BasePrice), new Money(75m, "JOD"));
        SetProperty(tour, nameof(Tour.Currency), "JOD");
        SetProperty(tour, nameof(Tour.Location), new Location(30.3285m, 35.4444m));
        SetProperty(tour, nameof(Tour.MeetingPoint), new Location(30.3220m, 35.4780m));
        SetProperty(tour, nameof(Tour.Status), TourStatus.Published);
        SetProperty(tour, nameof(Tour.IsFeatured), true);
        SetProperty(tour, nameof(Tour.IsInstantBooking), true);
        SetProperty(tour, nameof(Tour.CancellationPolicyHours), 24);
        SetProperty(tour, nameof(Tour.CreatedByUserId), GuideUserId);
        SetProperty(tour, nameof(Tour.PlaceId), SeedContentIds.PlacePetra);
        SetProperty(tour, nameof(Tour.IsChildFriendly), false);
        SetProperty(tour, nameof(Tour.IsAccessible), false);
        SetProperty(tour, nameof(Tour.AverageRating), 4.7m);
        SetProperty(tour, nameof(Tour.ReviewCount), 128);
        SetProperty(tour, nameof(Tour.BookingCount), 412);
        return tour;
    }

    private static List<TourTranslation> CreateTranslations()
    {
        var en = CreateEntity<TourTranslation>();
        SetProperty(en, nameof(TourTranslation.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(en, nameof(TourTranslation.LanguageId), LanguageEnglish);
        SetProperty(en, nameof(TourTranslation.Name), "Petra Full Day Explorer");
        SetProperty(en, nameof(TourTranslation.Description), "Discover Petra with a licensed local guide and curated route.");
        SetProperty(en, nameof(TourTranslation.ShortDescription), "Petra day tour.");
        SetProperty(en, nameof(TourTranslation.MeetingPoint), "Petra Visitor Center");

        var ar = CreateEntity<TourTranslation>();
        SetProperty(ar, nameof(TourTranslation.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(ar, nameof(TourTranslation.LanguageId), LanguageArabic);
        SetProperty(ar, nameof(TourTranslation.Name), "Petra Full Day Explorer");
        SetProperty(ar, nameof(TourTranslation.Description), "Guided Petra route with major highlights and local stories.");
        SetProperty(ar, nameof(TourTranslation.ShortDescription), "Petra guided day tour.");
        SetProperty(ar, nameof(TourTranslation.MeetingPoint), "Petra Visitor Center");

        return [en, ar];
    }

    private static List<TourSchedule> CreateSchedules()
    {
        var monday = CreateEntity<TourSchedule>();
        SetProperty(monday, nameof(TourSchedule.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(monday, nameof(TourSchedule.DayOfWeek), (byte)1);
        SetProperty(monday, nameof(TourSchedule.StartTime), new TimeOnly(8, 0));
        SetProperty(monday, nameof(TourSchedule.EndTime), new TimeOnly(16, 0));
        SetProperty(monday, nameof(TourSchedule.IsActive), true);

        var thursday = CreateEntity<TourSchedule>();
        SetProperty(thursday, nameof(TourSchedule.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(thursday, nameof(TourSchedule.DayOfWeek), (byte)4);
        SetProperty(thursday, nameof(TourSchedule.StartTime), new TimeOnly(8, 0));
        SetProperty(thursday, nameof(TourSchedule.EndTime), new TimeOnly(16, 0));
        SetProperty(thursday, nameof(TourSchedule.IsActive), true);

        return [monday, thursday];
    }

    private static List<TourWaypoint> CreateWaypoints()
    {
        var treasury = CreateEntity<TourWaypoint>();
        SetProperty(treasury, nameof(TourWaypoint.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(treasury, nameof(TourWaypoint.Name), "The Treasury");
        SetProperty(treasury, nameof(TourWaypoint.Description), "Main iconic facade stop.");
        SetProperty(treasury, nameof(TourWaypoint.Location), new Location(30.3289m, 35.4440m));
        SetProperty(treasury, nameof(TourWaypoint.SortOrder), 1);
        SetProperty(treasury, nameof(TourWaypoint.DurationMinutes), 45);
        SetProperty(treasury, nameof(TourWaypoint.WaypointType), (byte)0);

        var monastery = CreateEntity<TourWaypoint>();
        SetProperty(monastery, nameof(TourWaypoint.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(monastery, nameof(TourWaypoint.Name), "Ad Deir Monastery");
        SetProperty(monastery, nameof(TourWaypoint.Description), "High point with panoramic views.");
        SetProperty(monastery, nameof(TourWaypoint.Location), new Location(30.3392m, 35.4405m));
        SetProperty(monastery, nameof(TourWaypoint.SortOrder), 2);
        SetProperty(monastery, nameof(TourWaypoint.DurationMinutes), 60);
        SetProperty(monastery, nameof(TourWaypoint.WaypointType), (byte)0);

        return [treasury, monastery];
    }

    private static List<TourPricingTier> CreatePricingTiers()
    {
        var standard = CreateEntity<TourPricingTier>();
        SetProperty(standard, nameof(TourPricingTier.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(standard, nameof(TourPricingTier.Name), "Standard");
        SetProperty(standard, nameof(TourPricingTier.Description), "Core itinerary with guide.");
        SetProperty(standard, nameof(TourPricingTier.Price), new Money(75m, "JOD"));
        SetProperty(standard, nameof(TourPricingTier.Currency), "JOD");
        SetProperty(standard, nameof(TourPricingTier.MinParticipants), 1);
        SetProperty(standard, nameof(TourPricingTier.MaxParticipants), 18);
        SetProperty(standard, nameof(TourPricingTier.IsActive), true);

        var vip = CreateEntity<TourPricingTier>();
        SetProperty(vip, nameof(TourPricingTier.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(vip, nameof(TourPricingTier.Name), "VIP");
        SetProperty(vip, nameof(TourPricingTier.Description), "Private pace and upgraded transport.");
        SetProperty(vip, nameof(TourPricingTier.Price), new Money(130m, "JOD"));
        SetProperty(vip, nameof(TourPricingTier.Currency), "JOD");
        SetProperty(vip, nameof(TourPricingTier.MinParticipants), 1);
        SetProperty(vip, nameof(TourPricingTier.MaxParticipants), 6);
        SetProperty(vip, nameof(TourPricingTier.IsActive), true);

        return [standard, vip];
    }

    private static List<TourPackage> CreatePackages()
    {
        var package = CreateEntity<TourPackage>();
        SetProperty(package, nameof(TourPackage.Id), SeedContentIds.TourPackageEssentials);
        SetProperty(package, nameof(TourPackage.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(package, nameof(TourPackage.Name), "Petra Essentials");
        SetProperty(package, nameof(TourPackage.Description), "Guide, transport, and entry support.");
        SetProperty(package, nameof(TourPackage.Price), new Money(95m, "JOD"));
        SetProperty(package, nameof(TourPackage.Currency), "JOD");
        SetProperty(package, nameof(TourPackage.MaxParticipants), 18);
        SetProperty(package, nameof(TourPackage.ValidFrom), DateTime.UtcNow.AddDays(-30));
        SetProperty(package, nameof(TourPackage.ValidTo), DateTime.UtcNow.AddMonths(6));
        SetProperty(package, nameof(TourPackage.IsActive), true);
        return [package];
    }

    private static List<TourPackageInclusion> CreatePackageInclusions(IReadOnlyList<TourPackage> packages)
    {
        var packageId = packages[0].Id;

        var inclusion1 = CreateEntity<TourPackageInclusion>();
        SetProperty(inclusion1, nameof(TourPackageInclusion.TourPackageId), packageId);
        SetProperty(inclusion1, nameof(TourPackageInclusion.Description), "Licensed local guide");
        SetProperty(inclusion1, nameof(TourPackageInclusion.SortOrder), 1);

        var inclusion2 = CreateEntity<TourPackageInclusion>();
        SetProperty(inclusion2, nameof(TourPackageInclusion.TourPackageId), packageId);
        SetProperty(inclusion2, nameof(TourPackageInclusion.Description), "Hotel pickup in Petra area");
        SetProperty(inclusion2, nameof(TourPackageInclusion.SortOrder), 2);

        return [inclusion1, inclusion2];
    }

    private static List<TourTourGuide> CreateTourGuides()
    {
        var row = CreateEntity<TourTourGuide>();
        SetProperty(row, nameof(TourTourGuide.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(row, nameof(TourTourGuide.TourGuideId), GuideUserId);
        SetProperty(row, nameof(TourTourGuide.IsPrimary), true);
        return [row];
    }

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
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
