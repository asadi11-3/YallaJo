using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentTours.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-PKG — Development / QA-only seeder that creates 16 approved Jordan travel
/// <see cref="TourPackage"/> aggregates so the public <c>/packages</c> listing (grid/list,
/// toolbar, sort, min/max price filters, pagination, RTL/LTR) can be exercised end-to-end.
/// <para>
/// To vary the "N tours" badge (which projects <c>IncludedTours.Count</c>), this seeder first
/// ensures a small pool of lightweight shared dev tours exists (the 2 existing DEV-SEED-B1 tours
/// plus 5 extra minimal tours), then links each package to a distinct subset of 2–7 of them.
/// </para>
/// <para>
/// Guarded by <see cref="IHostEnvironment.IsDevelopment"/>; idempotent per-row (safe to run
/// repeatedly — existing rows are skipped by deterministic id). Runs after
/// <see cref="DevToursSeeder"/> (Order 163) via Order 164 so the base dev tours exist first.
/// </para>
/// </summary>
public sealed class DevPackagesSeeder(
    ContentToursDbContext dbContext,
    IHostEnvironment hostEnvironment,
    ILogger<DevPackagesSeeder> logger) : IModuleDbInitializer
{
    public int Order => 164;

    private static readonly Guid LanguageEnglish = new("eeeeeeee-0000-0000-0000-000000000001");

    // Extra lightweight shared dev tours (5eed0000-...-0008xxxxxxxx) used purely to vary the
    // IncludedTourCount badge across packages. Combined with the 2 existing DEV-SEED-B1 tours
    // this yields a pool of 7 distinct tour ids (enough for a 2–7 badge range).
    private static readonly Guid[] ExtraTourIds =
    [
        Guid.Parse("5eed0000-0000-0000-0000-000800000001"),
        Guid.Parse("5eed0000-0000-0000-0000-000800000002"),
        Guid.Parse("5eed0000-0000-0000-0000-000800000003"),
        Guid.Parse("5eed0000-0000-0000-0000-000800000004"),
        Guid.Parse("5eed0000-0000-0000-0000-000800000005"),
    ];

    // Pool of 7 distinct tour ids (existing 2 + extra 5) that packages draw from.
    private static Guid[] TourPool =>
    [
        DevSeedIds.TourWithImageId,
        DevSeedIds.TourWithoutImageId,
        .. ExtraTourIds,
    ];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var seededAny = false;

        // 1) Ensure the extra shared tours exist so package→tour FKs are valid and the
        //    IncludedTourCount badge can vary. (The 2 base tours come from DevToursSeeder.)
        for (var i = 0; i < ExtraTourIds.Length; i++)
        {
            seededAny |= await EnsureSharedTourAsync(
                ExtraTourIds[i],
                $"Dev Seed Package Tour {i + 1}",
                $"dev-seed-package-tour-{i + 1}",
                cancellationToken);
        }

        // 2) Seed the 16 Jordan travel packages.
        foreach (var spec in PackageSpecs)
        {
            seededAny |= await EnsurePackageAsync(spec, cancellationToken);
        }

        if (seededAny)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("DEV-SEED-PKG: seeded development Jordan travel packages.");
        }
    }

    private async Task<bool> EnsurePackageAsync(PackageSpec spec, CancellationToken cancellationToken)
    {
        var exists = await dbContext.TourPackages
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == spec.Id, cancellationToken);

        if (exists)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        var package = CreateEntity<TourPackage>();
        SetProperty(package, nameof(TourPackage.Id), spec.Id);
        SetProperty(package, nameof(TourPackage.CreatedByUserId), DevSeedIds.ProviderUserId);
        SetProperty(package, nameof(TourPackage.Name), spec.Name);
        SetProperty(package, nameof(TourPackage.Description), spec.Description);
        SetProperty(package, nameof(TourPackage.Price), new Money(spec.PriceJod, "JOD"));
        SetProperty(package, nameof(TourPackage.Currency), "JOD");
        SetProperty(package, nameof(TourPackage.MaxParticipants), (int?)spec.MaxParticipants);
        SetProperty(package, nameof(TourPackage.ValidFrom), (DateTime?)now.Date);
        SetProperty(package, nameof(TourPackage.ValidTo), (DateTime?)now.Date.AddMonths(spec.ValidMonths));
        SetProperty(package, nameof(TourPackage.IsActive), true);
        SetProperty(package, nameof(TourPackage.Status), TourPackageStatus.Approved);
        SetProperty(package, nameof(TourPackage.CoverImageUrl), spec.CoverImageUrl);
        SetProperty(package, nameof(TourPackage.CreatedAt), now);
        SetProperty(package, nameof(TourPackage.IsDeleted), false);

        dbContext.TourPackages.Add(package);

        // Link a distinct subset of the tour pool to drive the IncludedTourCount badge.
        var pool = TourPool;
        for (var i = 0; i < spec.IncludedTourCount && i < pool.Length; i++)
        {
            dbContext.TourPackageTours.Add(TourPackageTour.Create(spec.Id, pool[i]));
        }

        return true;
    }

    private async Task<bool> EnsureSharedTourAsync(
        Guid tourId,
        string name,
        string slug,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Tours
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == tourId, cancellationToken);

        if (exists)
        {
            return false;
        }

        const string description = "Development/QA seeded tour used to compose demo packages.";

        var tour = CreateEntity<Tour>();
        SetProperty(tour, nameof(Tour.Id), tourId);
        SetProperty(tour, nameof(Tour.Name), name);
        SetProperty(tour, nameof(Tour.Slug), slug);
        SetProperty(tour, nameof(Tour.Description), description);
        SetProperty(tour, nameof(Tour.ShortDescription), "Dev/QA seeded package tour.");
        SetProperty(tour, nameof(Tour.Difficulty), Difficulty.Easy);
        SetProperty(tour, nameof(Tour.DurationMinutes), 240);
        SetProperty(tour, nameof(Tour.MaxGroupSize), 12);
        SetProperty(tour, nameof(Tour.MinAge), (int?)null);
        SetProperty(tour, nameof(Tour.BasePrice), new Money(55m, "JOD"));
        SetProperty(tour, nameof(Tour.Currency), "JOD");
        SetProperty(tour, nameof(Tour.Location), new Location(30.3285m, 35.4444m));
        SetProperty(tour, nameof(Tour.MeetingPoint), new Location(30.3290m, 35.4450m));
        SetProperty(tour, nameof(Tour.Status), TourStatus.Approved);
        SetProperty(tour, nameof(Tour.IsFeatured), false);
        SetProperty(tour, nameof(Tour.IsInstantBooking), true);
        SetProperty(tour, nameof(Tour.CancellationPolicyHours), 24);
        SetProperty(tour, nameof(Tour.CreatedByUserId), DevSeedIds.GuideUserId);
        SetProperty(tour, nameof(Tour.PlaceId), DevSeedIds.PlaceId);
        SetProperty(tour, nameof(Tour.IsChildFriendly), true);
        SetProperty(tour, nameof(Tour.IsAccessible), true);
        SetProperty(tour, nameof(Tour.AverageRating), 4.6m);
        SetProperty(tour, nameof(Tour.ReviewCount), 4);
        SetProperty(tour, nameof(Tour.BookingCount), 2);

        dbContext.Tours.Add(tour);

        var translation = CreateEntity<TourTranslation>();
        SetProperty(translation, nameof(TourTranslation.TourId), tourId);
        SetProperty(translation, nameof(TourTranslation.LanguageId), LanguageEnglish);
        SetProperty(translation, nameof(TourTranslation.Name), name);
        SetProperty(translation, nameof(TourTranslation.Description), description);
        SetProperty(translation, nameof(TourTranslation.ShortDescription), "Dev/QA seeded package tour.");
        SetProperty(translation, nameof(TourTranslation.MeetingPoint), "Main visitor centre entrance.");
        dbContext.TourTranslations.Add(translation);

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

    private sealed record PackageSpec(
        Guid Id,
        string Name,
        string Description,
        decimal PriceJod,
        int IncludedTourCount,
        int MaxParticipants,
        int ValidMonths,
        string CoverImageUrl);

    private const string ImageBase = "/assets/images/jordan/packages/";

    // 16 Jordan travel packages. Deterministic ids in the 5eed0000-...-0009xxxxxxxx block.
    // IncludedTourCount varied 2–7, MaxParticipants varied 6–20, ValidMonths varied 6–12
    // (so the validity_ending_soon sort has a meaningful order). English copy only — the
    // TourPackage aggregate has no translations table.
    private static readonly PackageSpec[] PackageSpecs =
    [
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000001"),
            "Wadi Rum Desert & Bedouin Camp Package",
            "Sleep under the stars in a Bedouin camp, ride a 4x4 across the red dunes of Wadi Rum, and share sweet tea and grilled zarb with local hosts.",
            95m, 2, 12, 8, ImageBase + "package-wadi-rum.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000002"),
            "Petra, Wadi Rum & Dead Sea Classic Package",
            "Jordan's greatest hits in one trip: walk the Siq to Petra's Treasury, camp in Wadi Rum, and float weightlessly on the Dead Sea.",
            220m, 5, 16, 10, ImageBase + "package-petra-dead-sea.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000003"),
            "Amman, Jerash & Ajloun Heritage Package",
            "Explore the Roman colonnades of Jerash, the hilltop castle of Ajloun, and the bustling downtown and Citadel of modern Amman.",
            85m, 3, 18, 7, ImageBase + "package-jerash-ajloun.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000004"),
            "Aqaba Red Sea Escape Package",
            "Snorkel vibrant coral reefs, relax on the Gulf of Aqaba, and enjoy fresh seafood in Jordan's laid-back Red Sea resort town.",
            140m, 3, 14, 9, ImageBase + "package-aqaba.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000005"),
            "Dana Biosphere Hiking Package",
            "Trek Jordan's largest nature reserve through dramatic wadis and sandstone cliffs, spotting ibex and endemic birdlife with an expert guide.",
            120m, 4, 10, 11, ImageBase + "package-dana.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000006"),
            "Dead Sea Wellness & Floating Package",
            "Unwind at the lowest point on Earth with mineral-rich mud treatments, spa access, and an effortless float on the famous salt water.",
            110m, 2, 12, 6, ImageBase + "package-dead-sea.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000007"),
            "Madaba, Mount Nebo & Baptism Site Package",
            "Follow the mosaic map of Madaba, gaze over the Promised Land from Mount Nebo, and visit the Jordan River baptism site of Bethany.",
            75m, 3, 18, 7, ImageBase + "package-madaba-nebo.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000008"),
            "Petra Luxury Weekend Package",
            "A refined take on the Rose City: guided tour of Petra, a candle-lit Petra by Night experience, and a stay at a boutique desert retreat.",
            180m, 4, 8, 9, ImageBase + "package-petra-luxury.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000009"),
            "Jordan Family Discovery Package",
            "A relaxed, kid-friendly journey blending Petra, Wadi Rum jeep fun, and Dead Sea floating with comfortable pacing for all ages.",
            260m, 6, 20, 12, ImageBase + "package-family-jordan.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-00090000000a"),
            "Northern Jordan Castles & Nature Package",
            "Discover the green north: Ajloun's Ayyubid fortress, the Roman ruins of Umm Qais overlooking the Sea of Galilee, and rolling olive country.",
            130m, 4, 16, 8, ImageBase + "package-north-jordan.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-00090000000b"),
            "Wadi Mujib Adventure Package",
            "Scramble, swim, and canyon up the Siq Trail of Wadi Mujib — the spectacular river gorge that empties into the Dead Sea.",
            115m, 3, 10, 6, ImageBase + "package-wadi-mujib.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-00090000000c"),
            "Karak, Shobak & Petra History Package",
            "Trace the Crusader trail along the King's Highway through the mighty castles of Karak and Shobak, finishing at the wonder of Petra.",
            150m, 5, 16, 10, ImageBase + "package-karak-shobak.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-00090000000d"),
            "Desert Castles & Azraq Oasis Package",
            "Loop through the eastern desert to the Umayyad pleasure palaces of Qasr Amra and Qasr Kharana and the wetland oasis of Azraq.",
            90m, 3, 14, 7, ImageBase + "package-desert-castles.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-00090000000e"),
            "Amman Food, Culture & Downtown Package",
            "Taste your way through the capital: falafel and knafeh in downtown Amman, a Citadel and Roman Theatre visit, and a lively souk stroll.",
            65m, 2, 12, 6, ImageBase + "package-amman-food.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-00090000000f"),
            "Salt Heritage & Local Life Package",
            "Wander the honey-stone Ottoman streets of As-Salt, a UNESCO-listed town, meeting artisans and sharing a home-cooked Jordanian lunch.",
            70m, 2, 14, 7, ImageBase + "package-salt.webp"),
        new(
            Guid.Parse("5eed0000-0000-0000-0000-000900000010"),
            "Grand Jordan 7-Day Experience Package",
            "The complete Jordan odyssey: Amman, Jerash, the Dead Sea, Dana, Petra, Wadi Rum, and Aqaba across seven unforgettable days.",
            420m, 7, 18, 12, ImageBase + "package-grand-jordan.webp"),
    ];
}
