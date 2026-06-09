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
    private const string EnglishLanguageCode = "en";
    private const string ArabicLanguageCode = "ar";

    public int Order => 80;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Tours.AnyAsync(cancellationToken))
        {
            var tour = CreateTour();
            var translations = CreateTranslations();
            var schedules = CreateSchedules();
            var waypoints = CreateWaypoints();
            var pricingTiers = CreatePricingTiers();
            var tourGuides = CreateTourGuides();

            dbContext.Tours.Add(tour);
            dbContext.TourTranslations.AddRange(translations);
            dbContext.TourSchedules.AddRange(schedules);
            dbContext.TourWaypoints.AddRange(waypoints);
            dbContext.TourPricingTiers.AddRange(pricingTiers);
            dbContext.TourTourGuides.AddRange(tourGuides);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SeedTourChildFacilitiesAsync(cancellationToken);
        await SeedTourPricingTierTranslationsAsync(cancellationToken);
        await EnsureAdditionalToursAsync(cancellationToken);

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedTourChildFacilitiesAsync(CancellationToken cancellationToken)
    {
        await EnsureTourChildFacilitiesAsync(
            SeedContentIds.TourPetraExplorer,
            [
                ChildFacility.Stroller,
                ChildFacility.HighChair,
                ChildFacility.PlayArea,
                ChildFacility.ChildSeat
            ],
            cancellationToken);

        var secondTourId = await dbContext.Tours
            .Where(x => x.Id != SeedContentIds.TourPetraExplorer)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (secondTourId == Guid.Empty)
        {
            return;
        }

        await EnsureTourChildFacilitiesAsync(
            secondTourId,
            [
                ChildFacility.ChangingStation,
                ChildFacility.ChildMenu,
                ChildFacility.AirConditioning
            ],
            cancellationToken);
    }

    private async Task EnsureTourChildFacilitiesAsync(
        Guid tourId,
        IReadOnlyList<ChildFacility> facilities,
        CancellationToken cancellationToken)
    {
        foreach (var facility in facilities)
        {
            var exists = await dbContext.TourChildFacilities
                .AnyAsync(x => x.TourId == tourId && x.Facility == facility, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.TourChildFacilities.Add(new TourChildFacility(tourId, facility));
        }
    }

    private async Task SeedTourPricingTierTranslationsAsync(CancellationToken cancellationToken)
    {
        var existingTierNames = await dbContext.TourPricingTiers
            .Where(x => x.TourId == SeedContentIds.TourPetraExplorer)
            .Select(x => x.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        await TrySeedTierTranslationsByNameAsync(
            SeedContentIds.TourPetraExplorer,
            existingTierNames,
            "Adult",
            "Adult",
            "بالغ",
            "Standard adult ticket.",
            "تذكرة البالغين.",
            cancellationToken);

        await TrySeedTierTranslationsByNameAsync(
            SeedContentIds.TourPetraExplorer,
            existingTierNames,
            "Child",
            "Child",
            "طفل",
            "Standard child ticket.",
            "تذكرة الأطفال.",
            cancellationToken);

        await TrySeedTierTranslationsByNameAsync(
            SeedContentIds.TourPetraExplorer,
            existingTierNames,
            "Standard",
            "Standard",
            "قياسي",
            "Core itinerary with guide.",
            "المسار الأساسي مع دليل.",
            cancellationToken);

        await TrySeedTierTranslationsByNameAsync(
            SeedContentIds.TourPetraExplorer,
            existingTierNames,
            "VIP",
            "VIP",
            "كبار الشخصيات",
            "Premium tier with extra comfort.",
            "فئة مميزة مع راحة إضافية.",
            cancellationToken);
    }

    private async Task TrySeedTierTranslationsByNameAsync(
        Guid tourId,
        IReadOnlyCollection<string> existingTierNames,
        string tierName,
        string englishName,
        string arabicName,
        string englishDescription,
        string arabicDescription,
        CancellationToken cancellationToken)
    {
        if (!existingTierNames.Contains(tierName, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var tierId = await dbContext.TourPricingTiers
            .Where(x => x.TourId == tourId && x.Name == tierName)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        await EnsurePricingTierTranslationAsync(
            tierId,
            LanguageEnglish,
            EnglishLanguageCode,
            englishName,
            englishDescription,
            cancellationToken);

        await EnsurePricingTierTranslationAsync(
            tierId,
            LanguageArabic,
            ArabicLanguageCode,
            arabicName,
            arabicDescription,
            cancellationToken);
    }

    private async Task EnsurePricingTierTranslationAsync(
        Guid tierId,
        Guid languageId,
        string languageCode,
        string name,
        string description,
        CancellationToken cancellationToken)
    {
        var languageExistsOnTour = await dbContext.TourTranslations
            .AnyAsync(x => x.TourId == SeedContentIds.TourPetraExplorer && x.LanguageId == languageId, cancellationToken);

        if (!languageExistsOnTour)
        {
            return;
        }

        var exists = await dbContext.TourPricingTierTranslations
            .AnyAsync(x => x.TourPricingTierId == tierId && x.LanguageCode == languageCode, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.TourPricingTierTranslations.Add(TourPricingTierTranslation.Create(
            tierId,
            languageCode,
            name,
            description));
    }

    private async Task EnsureAdditionalToursAsync(CancellationToken cancellationToken)
    {
        // ── Dead Sea Day Trip ─────────────────────────────────────────────
        if (!await dbContext.Tours.AnyAsync(x => x.Id == SeedContentIds.TourDeadSeaDay, cancellationToken))
        {
            var deadSea = CreateEntity<Tour>();
            SetProperty(deadSea, nameof(Tour.Id),                    SeedContentIds.TourDeadSeaDay);
            SetProperty(deadSea, nameof(Tour.Name),                  "Dead Sea Floating Experience");
            SetProperty(deadSea, nameof(Tour.Slug),                  "dead-sea-floating-experience");
            SetProperty(deadSea, nameof(Tour.Description),           "A full-day trip to the Dead Sea including mud baths, floating, and a beach lunch.");
            SetProperty(deadSea, nameof(Tour.ShortDescription),      "Day trip to the Dead Sea with guided spa and float session.");
            SetProperty(deadSea, nameof(Tour.Difficulty),            Difficulty.Easy);
            SetProperty(deadSea, nameof(Tour.DurationMinutes),       480);
            SetProperty(deadSea, nameof(Tour.MaxGroupSize),          20);
            SetProperty(deadSea, nameof(Tour.MinAge),                5);
            SetProperty(deadSea, nameof(Tour.BasePrice),             new Money(55m, "JOD"));
            SetProperty(deadSea, nameof(Tour.Currency),              "JOD");
            SetProperty(deadSea, nameof(Tour.Location),              new Location(31.5590m, 35.4732m));
            SetProperty(deadSea, nameof(Tour.MeetingPoint),          new Location(31.9496m, 35.9328m));
            SetProperty(deadSea, nameof(Tour.Status),                TourStatus.Approved);
            SetProperty(deadSea, nameof(Tour.IsFeatured),            true);
            SetProperty(deadSea, nameof(Tour.IsInstantBooking),      true);
            SetProperty(deadSea, nameof(Tour.CancellationPolicyHours), 12);
            SetProperty(deadSea, nameof(Tour.CreatedByUserId),       GuideUserId);
            SetProperty(deadSea, nameof(Tour.PlaceId),               SeedContentIds.PlaceDeadSea);
            SetProperty(deadSea, nameof(Tour.IsChildFriendly),       true);
            SetProperty(deadSea, nameof(Tour.IsAccessible),          true);
            SetProperty(deadSea, nameof(Tour.AverageRating),         4.8m);
            SetProperty(deadSea, nameof(Tour.ReviewCount),           315);
            SetProperty(deadSea, nameof(Tour.BookingCount),          892);

            var deadSeaSchedule = CreateEntity<TourSchedule>();
            SetProperty(deadSeaSchedule, nameof(TourSchedule.TourId),    SeedContentIds.TourDeadSeaDay);
            SetProperty(deadSeaSchedule, nameof(TourSchedule.DayOfWeek), (byte)2); // Tuesday
            SetProperty(deadSeaSchedule, nameof(TourSchedule.StartTime), new TimeOnly(7, 30));
            SetProperty(deadSeaSchedule, nameof(TourSchedule.EndTime),   new TimeOnly(16, 0));
            SetProperty(deadSeaSchedule, nameof(TourSchedule.IsActive),  true);

            var deadSeaSchedule2 = CreateEntity<TourSchedule>();
            SetProperty(deadSeaSchedule2, nameof(TourSchedule.TourId),    SeedContentIds.TourDeadSeaDay);
            SetProperty(deadSeaSchedule2, nameof(TourSchedule.DayOfWeek), (byte)5); // Friday
            SetProperty(deadSeaSchedule2, nameof(TourSchedule.StartTime), new TimeOnly(7, 30));
            SetProperty(deadSeaSchedule2, nameof(TourSchedule.EndTime),   new TimeOnly(16, 0));
            SetProperty(deadSeaSchedule2, nameof(TourSchedule.IsActive),  true);

            var deadSeaTier = CreateEntity<TourPricingTier>();
            SetProperty(deadSeaTier, nameof(TourPricingTier.TourId),            SeedContentIds.TourDeadSeaDay);
            SetProperty(deadSeaTier, nameof(TourPricingTier.Name),              "Standard");
            SetProperty(deadSeaTier, nameof(TourPricingTier.Description),       "Includes entry, mud bath, lunch, and transport.");
            SetProperty(deadSeaTier, nameof(TourPricingTier.Price),             new Money(55m, "JOD"));
            SetProperty(deadSeaTier, nameof(TourPricingTier.ParticipantType),   ParticipantType.Adult);
            SetProperty(deadSeaTier, nameof(TourPricingTier.MinParticipants),   1);
            SetProperty(deadSeaTier, nameof(TourPricingTier.MaxParticipants),   20);
            SetProperty(deadSeaTier, nameof(TourPricingTier.IsActive),          true);

            var deadSeaGuide = CreateEntity<TourTourGuide>();
            SetProperty(deadSeaGuide, nameof(TourTourGuide.TourId),      SeedContentIds.TourDeadSeaDay);
            SetProperty(deadSeaGuide, nameof(TourTourGuide.TourGuideId), GuideUserId);
            SetProperty(deadSeaGuide, nameof(TourTourGuide.IsPrimary),   true);

            dbContext.Tours.Add(deadSea);
            dbContext.TourSchedules.AddRange(deadSeaSchedule, deadSeaSchedule2);
            dbContext.TourPricingTiers.Add(deadSeaTier);
            dbContext.TourTourGuides.Add(deadSeaGuide);
        }

        // ── Wadi Rum Desert Camp Tour ─────────────────────────────────────
        if (!await dbContext.Tours.AnyAsync(x => x.Id == SeedContentIds.TourWadiRumCamp, cancellationToken))
        {
            var wadiRum = CreateEntity<Tour>();
            SetProperty(wadiRum, nameof(Tour.Id),                    SeedContentIds.TourWadiRumCamp);
            SetProperty(wadiRum, nameof(Tour.Name),                  "Wadi Rum Jeep & Camp Overnight");
            SetProperty(wadiRum, nameof(Tour.Slug),                  "wadi-rum-jeep-camp-overnight");
            SetProperty(wadiRum, nameof(Tour.Description),           "Explore the majestic Wadi Rum desert by jeep and spend the night under a sky full of stars at a Bedouin camp.");
            SetProperty(wadiRum, nameof(Tour.ShortDescription),      "Jeep tour + Bedouin stargazing camp overnight in Wadi Rum.");
            SetProperty(wadiRum, nameof(Tour.Difficulty),            Difficulty.Easy);
            SetProperty(wadiRum, nameof(Tour.DurationMinutes),       1440); // 24 hrs
            SetProperty(wadiRum, nameof(Tour.MaxGroupSize),          14);
            SetProperty(wadiRum, nameof(Tour.MinAge),                8);
            SetProperty(wadiRum, nameof(Tour.BasePrice),             new Money(90m, "JOD"));
            SetProperty(wadiRum, nameof(Tour.Currency),              "JOD");
            SetProperty(wadiRum, nameof(Tour.Location),              new Location(29.5764m, 35.4203m));
            SetProperty(wadiRum, nameof(Tour.MeetingPoint),          new Location(29.6000m, 35.4167m));
            SetProperty(wadiRum, nameof(Tour.Status),                TourStatus.Approved);
            SetProperty(wadiRum, nameof(Tour.IsFeatured),            true);
            SetProperty(wadiRum, nameof(Tour.IsInstantBooking),      false);
            SetProperty(wadiRum, nameof(Tour.CancellationPolicyHours), 48);
            SetProperty(wadiRum, nameof(Tour.CreatedByUserId),       GuideUserId);
            SetProperty(wadiRum, nameof(Tour.PlaceId),               SeedContentIds.PlaceWadiRum);
            SetProperty(wadiRum, nameof(Tour.IsChildFriendly),       true);
            SetProperty(wadiRum, nameof(Tour.IsAccessible),          false);
            SetProperty(wadiRum, nameof(Tour.AverageRating),         4.9m);
            SetProperty(wadiRum, nameof(Tour.ReviewCount),           572);
            SetProperty(wadiRum, nameof(Tour.BookingCount),          1204);

            var wadiRumSchedule = CreateEntity<TourSchedule>();
            SetProperty(wadiRumSchedule, nameof(TourSchedule.TourId),    SeedContentIds.TourWadiRumCamp);
            SetProperty(wadiRumSchedule, nameof(TourSchedule.DayOfWeek), (byte)3); // Wednesday
            SetProperty(wadiRumSchedule, nameof(TourSchedule.StartTime), new TimeOnly(9, 0));
            SetProperty(wadiRumSchedule, nameof(TourSchedule.EndTime),   new TimeOnly(9, 0)); // next day
            SetProperty(wadiRumSchedule, nameof(TourSchedule.IsActive),  true);

            var wadiRumTier = CreateEntity<TourPricingTier>();
            SetProperty(wadiRumTier, nameof(TourPricingTier.TourId),            SeedContentIds.TourWadiRumCamp);
            SetProperty(wadiRumTier, nameof(TourPricingTier.Name),              "Standard");
            SetProperty(wadiRumTier, nameof(TourPricingTier.Description),       "Includes jeep tour, Bedouin dinner, and overnight stay.");
            SetProperty(wadiRumTier, nameof(TourPricingTier.Price),             new Money(90m, "JOD"));
            SetProperty(wadiRumTier, nameof(TourPricingTier.ParticipantType),   ParticipantType.Adult);
            SetProperty(wadiRumTier, nameof(TourPricingTier.MinParticipants),   1);
            SetProperty(wadiRumTier, nameof(TourPricingTier.MaxParticipants),   14);
            SetProperty(wadiRumTier, nameof(TourPricingTier.IsActive),          true);

            var wadiRumGuide = CreateEntity<TourTourGuide>();
            SetProperty(wadiRumGuide, nameof(TourTourGuide.TourId),      SeedContentIds.TourWadiRumCamp);
            SetProperty(wadiRumGuide, nameof(TourTourGuide.TourGuideId), GuideUserId);
            SetProperty(wadiRumGuide, nameof(TourTourGuide.IsPrimary),   true);

            dbContext.Tours.Add(wadiRum);
            dbContext.TourSchedules.Add(wadiRumSchedule);
            dbContext.TourPricingTiers.Add(wadiRumTier);
            dbContext.TourTourGuides.Add(wadiRumGuide);
        }

        // ── Amman City Walking Tour ────────────────────────────────────────
        if (!await dbContext.Tours.AnyAsync(x => x.Id == SeedContentIds.TourAmmanCity, cancellationToken))
        {
            var amman = CreateEntity<Tour>();
            SetProperty(amman, nameof(Tour.Id),                    SeedContentIds.TourAmmanCity);
            SetProperty(amman, nameof(Tour.Name),                  "Amman Old City Walking Tour");
            SetProperty(amman, nameof(Tour.Slug),                  "amman-old-city-walking-tour");
            SetProperty(amman, nameof(Tour.Description),           "A half-day walking tour through downtown Amman's historic souks, Roman theatre, and street-food hotspots.");
            SetProperty(amman, nameof(Tour.ShortDescription),      "Historic downtown walk with street food tasting in Amman.");
            SetProperty(amman, nameof(Tour.Difficulty),            Difficulty.Easy);
            SetProperty(amman, nameof(Tour.DurationMinutes),       240);
            SetProperty(amman, nameof(Tour.MaxGroupSize),          15);
            SetProperty(amman, nameof(Tour.MinAge),                (int?)null);
            SetProperty(amman, nameof(Tour.BasePrice),             new Money(35m, "JOD"));
            SetProperty(amman, nameof(Tour.Currency),              "JOD");
            SetProperty(amman, nameof(Tour.Location),              new Location(31.9496m, 35.9328m));
            SetProperty(amman, nameof(Tour.MeetingPoint),          new Location(31.9530m, 35.9310m));
            SetProperty(amman, nameof(Tour.Status),                TourStatus.Approved);
            SetProperty(amman, nameof(Tour.IsFeatured),            false);
            SetProperty(amman, nameof(Tour.IsInstantBooking),      true);
            SetProperty(amman, nameof(Tour.CancellationPolicyHours), 6);
            SetProperty(amman, nameof(Tour.CreatedByUserId),       GuideUserId);
            SetProperty(amman, nameof(Tour.PlaceId),               SeedContentIds.PlaceAmman);
            SetProperty(amman, nameof(Tour.IsChildFriendly),       true);
            SetProperty(amman, nameof(Tour.IsAccessible),          true);
            SetProperty(amman, nameof(Tour.AverageRating),         4.6m);
            SetProperty(amman, nameof(Tour.ReviewCount),           204);
            SetProperty(amman, nameof(Tour.BookingCount),          631);

            var ammanSchedule1 = CreateEntity<TourSchedule>();
            SetProperty(ammanSchedule1, nameof(TourSchedule.TourId),    SeedContentIds.TourAmmanCity);
            SetProperty(ammanSchedule1, nameof(TourSchedule.DayOfWeek), (byte)1); // Monday
            SetProperty(ammanSchedule1, nameof(TourSchedule.StartTime), new TimeOnly(9, 0));
            SetProperty(ammanSchedule1, nameof(TourSchedule.EndTime),   new TimeOnly(13, 0));
            SetProperty(ammanSchedule1, nameof(TourSchedule.IsActive),  true);

            var ammanSchedule2 = CreateEntity<TourSchedule>();
            SetProperty(ammanSchedule2, nameof(TourSchedule.TourId),    SeedContentIds.TourAmmanCity);
            SetProperty(ammanSchedule2, nameof(TourSchedule.DayOfWeek), (byte)3); // Wednesday
            SetProperty(ammanSchedule2, nameof(TourSchedule.StartTime), new TimeOnly(9, 0));
            SetProperty(ammanSchedule2, nameof(TourSchedule.EndTime),   new TimeOnly(13, 0));
            SetProperty(ammanSchedule2, nameof(TourSchedule.IsActive),  true);

            var ammanSchedule3 = CreateEntity<TourSchedule>();
            SetProperty(ammanSchedule3, nameof(TourSchedule.TourId),    SeedContentIds.TourAmmanCity);
            SetProperty(ammanSchedule3, nameof(TourSchedule.DayOfWeek), (byte)6); // Saturday
            SetProperty(ammanSchedule3, nameof(TourSchedule.StartTime), new TimeOnly(10, 0));
            SetProperty(ammanSchedule3, nameof(TourSchedule.EndTime),   new TimeOnly(14, 0));
            SetProperty(ammanSchedule3, nameof(TourSchedule.IsActive),  true);

            var ammanTierAdult = CreateEntity<TourPricingTier>();
            SetProperty(ammanTierAdult, nameof(TourPricingTier.TourId),          SeedContentIds.TourAmmanCity);
            SetProperty(ammanTierAdult, nameof(TourPricingTier.Name),            "Adult");
            SetProperty(ammanTierAdult, nameof(TourPricingTier.Description),     "Includes local guide, street food tasting, and museum entry.");
            SetProperty(ammanTierAdult, nameof(TourPricingTier.Price),           new Money(35m, "JOD"));
            SetProperty(ammanTierAdult, nameof(TourPricingTier.ParticipantType), ParticipantType.Adult);
            SetProperty(ammanTierAdult, nameof(TourPricingTier.MinParticipants), 1);
            SetProperty(ammanTierAdult, nameof(TourPricingTier.MaxParticipants), 15);
            SetProperty(ammanTierAdult, nameof(TourPricingTier.IsActive),        true);

            var ammanTierChild = CreateEntity<TourPricingTier>();
            SetProperty(ammanTierChild, nameof(TourPricingTier.TourId),          SeedContentIds.TourAmmanCity);
            SetProperty(ammanTierChild, nameof(TourPricingTier.Name),            "Child");
            SetProperty(ammanTierChild, nameof(TourPricingTier.Description),     "Ages 5–12 accompanied by an adult.");
            SetProperty(ammanTierChild, nameof(TourPricingTier.Price),           new Money(18m, "JOD"));
            SetProperty(ammanTierChild, nameof(TourPricingTier.ParticipantType), ParticipantType.Child);
            SetProperty(ammanTierChild, nameof(TourPricingTier.MinParticipants), 1);
            SetProperty(ammanTierChild, nameof(TourPricingTier.MaxParticipants), 15);
            SetProperty(ammanTierChild, nameof(TourPricingTier.IsActive),        true);

            var ammanGuide = CreateEntity<TourTourGuide>();
            SetProperty(ammanGuide, nameof(TourTourGuide.TourId),      SeedContentIds.TourAmmanCity);
            SetProperty(ammanGuide, nameof(TourTourGuide.TourGuideId), GuideUserId);
            SetProperty(ammanGuide, nameof(TourTourGuide.IsPrimary),   true);

            dbContext.Tours.Add(amman);
            dbContext.TourSchedules.AddRange(ammanSchedule1, ammanSchedule2, ammanSchedule3);
            dbContext.TourPricingTiers.AddRange(ammanTierAdult, ammanTierChild);
            dbContext.TourTourGuides.Add(ammanGuide);
        }
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
        SetProperty(tour, nameof(Tour.Status), TourStatus.Approved);
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
        SetProperty(standard, nameof(TourPricingTier.ParticipantType), ParticipantType.Adult);
        SetProperty(standard, nameof(TourPricingTier.MinParticipants), 1);
        SetProperty(standard, nameof(TourPricingTier.MaxParticipants), 18);
        SetProperty(standard, nameof(TourPricingTier.IsActive), true);

        var vip = CreateEntity<TourPricingTier>();
        SetProperty(vip, nameof(TourPricingTier.TourId), SeedContentIds.TourPetraExplorer);
        SetProperty(vip, nameof(TourPricingTier.Name), "VIP");
        SetProperty(vip, nameof(TourPricingTier.Description), "Private pace and upgraded transport.");
        SetProperty(vip, nameof(TourPricingTier.Price), new Money(130m, "JOD"));
        SetProperty(vip, nameof(TourPricingTier.ParticipantType), ParticipantType.Other);
        SetProperty(vip, nameof(TourPricingTier.MinParticipants), 1);
        SetProperty(vip, nameof(TourPricingTier.MaxParticipants), 6);
        SetProperty(vip, nameof(TourPricingTier.IsActive), true);

        return [standard, vip];
    }

    // Task 5 — Package seeders intentionally removed (see initializer note above).

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
