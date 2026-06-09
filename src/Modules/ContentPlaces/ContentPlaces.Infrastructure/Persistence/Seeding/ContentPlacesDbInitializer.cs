using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;
using PlaceDayOfWeek = ContentPlaces.Domain.Enums.DayOfWeek;

namespace ContentPlaces.Infrastructure.Persistence.Seeding;

public sealed class ContentPlacesDbInitializer(ContentPlacesDbContext dbContext) : IModuleDbInitializer
{
    public int Order => 60;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Places.AnyAsync(cancellationToken))
        {
            return;
        }

        var places         = CreatePlaces();
        var businesses     = CreateBusinesses();
        var placeBusinesses = CreatePlaceBusinesses();
        var amenities      = CreateAmenities();
        var hours          = CreateBusinessHours();

        dbContext.Places.AddRange(places);
        dbContext.Businesses.AddRange(businesses);
        dbContext.PlaceBusinesses.AddRange(placeBusinesses);
        dbContext.BusinessAmenities.AddRange(amenities);
        dbContext.BusinessHours.AddRange(hours);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Place> CreatePlaces()
    {
        // ── Petra ─────────────────────────────────────────────────────────
        var petra = CreateEntity<Place>();
        SetProperty(petra, nameof(Place.Id),                   SeedContentIds.PlacePetra);
        SetProperty(petra, nameof(Place.Name),                 "Petra Archaeological Park");
        SetProperty(petra, nameof(Place.Slug),                 "petra-archaeological-park");
        SetProperty(petra, nameof(Place.Description),          "Historic Nabatean city and UNESCO World Heritage site.");
        SetProperty(petra, nameof(Place.PlaceType),            PlaceType.Historical);
        SetProperty(petra, nameof(Place.Location),             new Location(30.3285m, 35.4444m));
        SetProperty(petra, nameof(Place.City),                 "Petra");
        SetProperty(petra, nameof(Place.Country),              "Jordan");
        SetProperty(petra, nameof(Place.AverageRating),        4.8m);
        SetProperty(petra, nameof(Place.ReviewCount),          1240);
        SetProperty(petra, nameof(Place.IsFeatured),           true);
        SetProperty(petra, nameof(Place.IsVerified),           true);
        SetProperty(petra, nameof(Place.IsWheelchairAccessible), false);
        SetProperty(petra, nameof(Place.HasAudioGuide),        true);
        SetProperty(petra, nameof(Place.HasBrailleSignage),    false);
        SetProperty(petra, nameof(Place.CreatedByUserId),      SeedIdentityProfiles.All[0].UserId);

        // ── Jerash ────────────────────────────────────────────────────────
        var jerash = CreateEntity<Place>();
        SetProperty(jerash, nameof(Place.Id),                  SeedContentIds.PlaceJerash);
        SetProperty(jerash, nameof(Place.Name),                "Jerash Roman Ruins");
        SetProperty(jerash, nameof(Place.Slug),                "jerash-roman-ruins");
        SetProperty(jerash, nameof(Place.Description),         "One of the largest and best-preserved Roman sites outside Italy.");
        SetProperty(jerash, nameof(Place.PlaceType),           PlaceType.Historical);
        SetProperty(jerash, nameof(Place.Location),            new Location(32.2744m, 35.8914m));
        SetProperty(jerash, nameof(Place.City),                "Jerash");
        SetProperty(jerash, nameof(Place.Country),             "Jordan");
        SetProperty(jerash, nameof(Place.AverageRating),       4.6m);
        SetProperty(jerash, nameof(Place.ReviewCount),         810);
        SetProperty(jerash, nameof(Place.IsFeatured),          false);
        SetProperty(jerash, nameof(Place.IsVerified),          true);
        SetProperty(jerash, nameof(Place.IsWheelchairAccessible), true);
        SetProperty(jerash, nameof(Place.HasAudioGuide),       true);
        SetProperty(jerash, nameof(Place.HasBrailleSignage),   false);
        SetProperty(jerash, nameof(Place.CreatedByUserId),     SeedIdentityProfiles.All[0].UserId);

        // ── Dead Sea ──────────────────────────────────────────────────────
        var deadSea = CreateEntity<Place>();
        SetProperty(deadSea, nameof(Place.Id),                 SeedContentIds.PlaceDeadSea);
        SetProperty(deadSea, nameof(Place.Name),               "Dead Sea");
        SetProperty(deadSea, nameof(Place.Slug),               "dead-sea");
        SetProperty(deadSea, nameof(Place.Description),        "The saltiest body of water on Earth, famous for its mineral-rich mud and effortless floating.");
        SetProperty(deadSea, nameof(Place.PlaceType),          PlaceType.Nature);
        SetProperty(deadSea, nameof(Place.Location),           new Location(31.5590m, 35.4732m));
        SetProperty(deadSea, nameof(Place.City),               "Dead Sea");
        SetProperty(deadSea, nameof(Place.Country),            "Jordan");
        SetProperty(deadSea, nameof(Place.AverageRating),      4.7m);
        SetProperty(deadSea, nameof(Place.ReviewCount),        2100);
        SetProperty(deadSea, nameof(Place.IsFeatured),         true);
        SetProperty(deadSea, nameof(Place.IsVerified),         true);
        SetProperty(deadSea, nameof(Place.IsWheelchairAccessible), true);
        SetProperty(deadSea, nameof(Place.HasAudioGuide),      false);
        SetProperty(deadSea, nameof(Place.HasBrailleSignage),  false);
        SetProperty(deadSea, nameof(Place.CreatedByUserId),    SeedIdentityProfiles.All[0].UserId);

        // ── Wadi Rum ──────────────────────────────────────────────────────
        var wadiRum = CreateEntity<Place>();
        SetProperty(wadiRum, nameof(Place.Id),                 SeedContentIds.PlaceWadiRum);
        SetProperty(wadiRum, nameof(Place.Name),               "Wadi Rum Desert");
        SetProperty(wadiRum, nameof(Place.Slug),               "wadi-rum-desert");
        SetProperty(wadiRum, nameof(Place.Description),        "A spectacular desert valley known as the Valley of the Moon, with towering sandstone mountains and ancient petroglyphs.");
        SetProperty(wadiRum, nameof(Place.PlaceType),          PlaceType.Nature);
        SetProperty(wadiRum, nameof(Place.Location),           new Location(29.5764m, 35.4203m));
        SetProperty(wadiRum, nameof(Place.City),               "Wadi Rum");
        SetProperty(wadiRum, nameof(Place.Country),            "Jordan");
        SetProperty(wadiRum, nameof(Place.AverageRating),      4.9m);
        SetProperty(wadiRum, nameof(Place.ReviewCount),        1870);
        SetProperty(wadiRum, nameof(Place.IsFeatured),         true);
        SetProperty(wadiRum, nameof(Place.IsVerified),         true);
        SetProperty(wadiRum, nameof(Place.IsWheelchairAccessible), false);
        SetProperty(wadiRum, nameof(Place.HasAudioGuide),      false);
        SetProperty(wadiRum, nameof(Place.HasBrailleSignage),  false);
        SetProperty(wadiRum, nameof(Place.CreatedByUserId),    SeedIdentityProfiles.All[0].UserId);

        // ── Aqaba ─────────────────────────────────────────────────────────
        var aqaba = CreateEntity<Place>();
        SetProperty(aqaba, nameof(Place.Id),                   SeedContentIds.PlaceAqaba);
        SetProperty(aqaba, nameof(Place.Name),                 "Aqaba");
        SetProperty(aqaba, nameof(Place.Slug),                 "aqaba");
        SetProperty(aqaba, nameof(Place.Description),          "Jordan's only coastal city on the Red Sea, offering vibrant coral reefs, diving and a lively waterfront.");
        SetProperty(aqaba, nameof(Place.PlaceType),            PlaceType.Nature);
        SetProperty(aqaba, nameof(Place.Location),             new Location(29.5269m, 35.0060m));
        SetProperty(aqaba, nameof(Place.City),                 "Aqaba");
        SetProperty(aqaba, nameof(Place.Country),              "Jordan");
        SetProperty(aqaba, nameof(Place.AverageRating),        4.5m);
        SetProperty(aqaba, nameof(Place.ReviewCount),          930);
        SetProperty(aqaba, nameof(Place.IsFeatured),           false);
        SetProperty(aqaba, nameof(Place.IsVerified),           true);
        SetProperty(aqaba, nameof(Place.IsWheelchairAccessible), true);
        SetProperty(aqaba, nameof(Place.HasAudioGuide),        false);
        SetProperty(aqaba, nameof(Place.HasBrailleSignage),    false);
        SetProperty(aqaba, nameof(Place.CreatedByUserId),      SeedIdentityProfiles.All[0].UserId);

        // ── Amman ─────────────────────────────────────────────────────────
        var amman = CreateEntity<Place>();
        SetProperty(amman, nameof(Place.Id),                   SeedContentIds.PlaceAmman);
        SetProperty(amman, nameof(Place.Name),                 "Amman");
        SetProperty(amman, nameof(Place.Slug),                 "amman");
        SetProperty(amman, nameof(Place.Description),          "Jordan's vibrant capital city, blending ancient history with modern culture, street food and bustling souks.");
        SetProperty(amman, nameof(Place.PlaceType),            PlaceType.Historical);
        SetProperty(amman, nameof(Place.Location),             new Location(31.9496m, 35.9328m));
        SetProperty(amman, nameof(Place.City),                 "Amman");
        SetProperty(amman, nameof(Place.Country),              "Jordan");
        SetProperty(amman, nameof(Place.AverageRating),        4.4m);
        SetProperty(amman, nameof(Place.ReviewCount),          640);
        SetProperty(amman, nameof(Place.IsFeatured),           false);
        SetProperty(amman, nameof(Place.IsVerified),           true);
        SetProperty(amman, nameof(Place.IsWheelchairAccessible), true);
        SetProperty(amman, nameof(Place.HasAudioGuide),        true);
        SetProperty(amman, nameof(Place.HasBrailleSignage),    false);
        SetProperty(amman, nameof(Place.CreatedByUserId),      SeedIdentityProfiles.All[0].UserId);

        return [petra, jerash, deadSea, wadiRum, aqaba, amman];
    }

    private static List<Business> CreateBusinesses()
    {
        // ── Petra Local Guides ────────────────────────────────────────────
        var petraGuides = CreateEntity<Business>();
        SetProperty(petraGuides, nameof(Business.Id),          SeedContentIds.BusinessPetraGuides);
        SetProperty(petraGuides, nameof(Business.Name),        "Petra Local Guides");
        SetProperty(petraGuides, nameof(Business.Slug),        "petra-local-guides");
        SetProperty(petraGuides, nameof(Business.Description), "Licensed local guides for Petra and Wadi Musa experiences.");
        SetProperty(petraGuides, nameof(Business.BusinessType), BusinessType.Guide);
        SetProperty(petraGuides, nameof(Business.PlaceId),     SeedContentIds.PlacePetra);
        SetProperty(petraGuides, nameof(Business.Location),    new Location(30.3220m, 35.4780m));
        SetProperty(petraGuides, nameof(Business.City),        "Wadi Musa");
        SetProperty(petraGuides, nameof(Business.Country),     "Jordan");
        SetProperty(petraGuides, nameof(Business.Email),       "contact@petralocalguides.jo");
        SetProperty(petraGuides, nameof(Business.Phone),       "+962790000001");
        SetProperty(petraGuides, nameof(Business.AverageRating), 4.7m);
        SetProperty(petraGuides, nameof(Business.ReviewCount), 320);
        SetProperty(petraGuides, nameof(Business.IsVerified),  true);
        SetProperty(petraGuides, nameof(Business.IsFeatured),  true);
        SetProperty(petraGuides, nameof(Business.OwnerId),     SeedIdentityProfiles.All[3].UserId);
        SetProperty(petraGuides, nameof(Business.Status),      BusinessStatus.Approved);

        // ── Amman Food Walks ──────────────────────────────────────────────
        var ammanFood = CreateEntity<Business>();
        SetProperty(ammanFood, nameof(Business.Id),            SeedContentIds.BusinessAmmanFood);
        SetProperty(ammanFood, nameof(Business.Name),          "Amman Food Walks");
        SetProperty(ammanFood, nameof(Business.Slug),          "amman-food-walks");
        SetProperty(ammanFood, nameof(Business.Description),   "Street-food and local cuisine guided walks in downtown Amman.");
        SetProperty(ammanFood, nameof(Business.BusinessType),  BusinessType.Agency);
        SetProperty(ammanFood, nameof(Business.PlaceId),       SeedContentIds.PlaceAmman);
        SetProperty(ammanFood, nameof(Business.Location),      new Location(31.9516m, 35.9239m));
        SetProperty(ammanFood, nameof(Business.City),          "Amman");
        SetProperty(ammanFood, nameof(Business.Country),       "Jordan");
        SetProperty(ammanFood, nameof(Business.Email),         "hello@ammanfoodwalks.jo");
        SetProperty(ammanFood, nameof(Business.Phone),         "+962790000002");
        SetProperty(ammanFood, nameof(Business.AverageRating), 4.5m);
        SetProperty(ammanFood, nameof(Business.ReviewCount),   210);
        SetProperty(ammanFood, nameof(Business.IsVerified),    true);
        SetProperty(ammanFood, nameof(Business.IsFeatured),    false);
        SetProperty(ammanFood, nameof(Business.OwnerId),       SeedIdentityProfiles.All[4].UserId);
        SetProperty(ammanFood, nameof(Business.Status),        BusinessStatus.Approved);

        // ── Dead Sea Resort Spa ───────────────────────────────────────────
        var deadSeaResort = CreateEntity<Business>();
        SetProperty(deadSeaResort, nameof(Business.Id),        SeedContentIds.BusinessDeadSeaResort);
        SetProperty(deadSeaResort, nameof(Business.Name),      "Dead Sea Wellness Resort");
        SetProperty(deadSeaResort, nameof(Business.Slug),      "dead-sea-wellness-resort");
        SetProperty(deadSeaResort, nameof(Business.Description), "Luxury spa and wellness experiences on the shores of the Dead Sea.");
        SetProperty(deadSeaResort, nameof(Business.BusinessType), BusinessType.Hotel);
        SetProperty(deadSeaResort, nameof(Business.PlaceId),   SeedContentIds.PlaceDeadSea);
        SetProperty(deadSeaResort, nameof(Business.Location),  new Location(31.5560m, 35.4700m));
        SetProperty(deadSeaResort, nameof(Business.City),      "Dead Sea");
        SetProperty(deadSeaResort, nameof(Business.Country),   "Jordan");
        SetProperty(deadSeaResort, nameof(Business.Email),     "bookings@deadseaspa.jo");
        SetProperty(deadSeaResort, nameof(Business.Phone),     "+962790000003");
        SetProperty(deadSeaResort, nameof(Business.AverageRating), 4.8m);
        SetProperty(deadSeaResort, nameof(Business.ReviewCount), 540);
        SetProperty(deadSeaResort, nameof(Business.IsVerified), true);
        SetProperty(deadSeaResort, nameof(Business.IsFeatured), true);
        SetProperty(deadSeaResort, nameof(Business.OwnerId),   SeedIdentityProfiles.All[0].UserId);
        SetProperty(deadSeaResort, nameof(Business.Status),    BusinessStatus.Approved);

        // ── Wadi Rum Bedouin Camp ─────────────────────────────────────────
        var wadiRumCamp = CreateEntity<Business>();
        SetProperty(wadiRumCamp, nameof(Business.Id),          SeedContentIds.BusinessWadiRumCamp);
        SetProperty(wadiRumCamp, nameof(Business.Name),        "Wadi Rum Bedouin Camp");
        SetProperty(wadiRumCamp, nameof(Business.Slug),        "wadi-rum-bedouin-camp");
        SetProperty(wadiRumCamp, nameof(Business.Description), "Authentic Bedouin desert camp with stargazing, jeep tours, and camel rides.");
        SetProperty(wadiRumCamp, nameof(Business.BusinessType), BusinessType.Agency);
        SetProperty(wadiRumCamp, nameof(Business.PlaceId),     SeedContentIds.PlaceWadiRum);
        SetProperty(wadiRumCamp, nameof(Business.Location),    new Location(29.5800m, 35.4230m));
        SetProperty(wadiRumCamp, nameof(Business.City),        "Wadi Rum");
        SetProperty(wadiRumCamp, nameof(Business.Country),     "Jordan");
        SetProperty(wadiRumCamp, nameof(Business.Email),       "camp@wadirum-bedouin.jo");
        SetProperty(wadiRumCamp, nameof(Business.Phone),       "+962790000004");
        SetProperty(wadiRumCamp, nameof(Business.AverageRating), 4.9m);
        SetProperty(wadiRumCamp, nameof(Business.ReviewCount), 780);
        SetProperty(wadiRumCamp, nameof(Business.IsVerified),  true);
        SetProperty(wadiRumCamp, nameof(Business.IsFeatured),  true);
        SetProperty(wadiRumCamp, nameof(Business.OwnerId),     SeedIdentityProfiles.All[0].UserId);
        SetProperty(wadiRumCamp, nameof(Business.Status),      BusinessStatus.Approved);

        // ── Aqaba Divers ──────────────────────────────────────────────────
        var aqabaDivers = CreateEntity<Business>();
        SetProperty(aqabaDivers, nameof(Business.Id),          SeedContentIds.BusinessAqabaDivers);
        SetProperty(aqabaDivers, nameof(Business.Name),        "Aqaba Red Sea Divers");
        SetProperty(aqabaDivers, nameof(Business.Slug),        "aqaba-red-sea-divers");
        SetProperty(aqabaDivers, nameof(Business.Description), "PADI-certified dive centre offering reef dives, snorkelling trips, and night dives in the Red Sea.");
        SetProperty(aqabaDivers, nameof(Business.BusinessType), BusinessType.Guide);
        SetProperty(aqabaDivers, nameof(Business.PlaceId),     SeedContentIds.PlaceAqaba);
        SetProperty(aqabaDivers, nameof(Business.Location),    new Location(29.5220m, 35.0020m));
        SetProperty(aqabaDivers, nameof(Business.City),        "Aqaba");
        SetProperty(aqabaDivers, nameof(Business.Country),     "Jordan");
        SetProperty(aqabaDivers, nameof(Business.Email),       "dive@aqabaredsea.jo");
        SetProperty(aqabaDivers, nameof(Business.Phone),       "+962790000005");
        SetProperty(aqabaDivers, nameof(Business.AverageRating), 4.6m);
        SetProperty(aqabaDivers, nameof(Business.ReviewCount), 390);
        SetProperty(aqabaDivers, nameof(Business.IsVerified),  true);
        SetProperty(aqabaDivers, nameof(Business.IsFeatured),  false);
        SetProperty(aqabaDivers, nameof(Business.OwnerId),     SeedIdentityProfiles.All[0].UserId);
        SetProperty(aqabaDivers, nameof(Business.Status),      BusinessStatus.Approved);

        return [petraGuides, ammanFood, deadSeaResort, wadiRumCamp, aqabaDivers];
    }

    private static List<PlaceBusiness> CreatePlaceBusinesses()
    {
        PlaceBusiness Make(Guid placeId, Guid businessId)
        {
            var pb = CreateEntity<PlaceBusiness>();
            SetProperty(pb, nameof(PlaceBusiness.PlaceId),    placeId);
            SetProperty(pb, nameof(PlaceBusiness.BusinessId), businessId);
            return pb;
        }

        return
        [
            Make(SeedContentIds.PlacePetra,   SeedContentIds.BusinessPetraGuides),
            Make(SeedContentIds.PlaceAmman,   SeedContentIds.BusinessAmmanFood),
            Make(SeedContentIds.PlaceDeadSea, SeedContentIds.BusinessDeadSeaResort),
            Make(SeedContentIds.PlaceWadiRum, SeedContentIds.BusinessWadiRumCamp),
            Make(SeedContentIds.PlaceAqaba,   SeedContentIds.BusinessAqabaDivers),
        ];
    }

    private static List<BusinessAmenity> CreateAmenities()
    {
        BusinessAmenity Make(Guid businessId, string name, string icon, int order)
        {
            var a = CreateEntity<BusinessAmenity>();
            SetProperty(a, nameof(BusinessAmenity.BusinessId), businessId);
            SetProperty(a, nameof(BusinessAmenity.Name),       name);
            SetProperty(a, nameof(BusinessAmenity.Icon),       icon);
            SetProperty(a, nameof(BusinessAmenity.SortOrder),  order);
            return a;
        }

        return
        [
            Make(SeedContentIds.BusinessPetraGuides,   "Wifi",          "wifi",    1),
            Make(SeedContentIds.BusinessAmmanFood,     "Parking",       "parking", 1),
            Make(SeedContentIds.BusinessDeadSeaResort, "Pool",          "water",   1),
            Make(SeedContentIds.BusinessDeadSeaResort, "Spa",           "spa",     2),
            Make(SeedContentIds.BusinessWadiRumCamp,   "Camping Gear",  "tent",    1),
            Make(SeedContentIds.BusinessAqabaDivers,   "Dive Equipment","anchor",  1),
        ];
    }

    private static List<BusinessHours> CreateBusinessHours()
    {
        var results = new List<BusinessHours>();

        // PetraGuides: Sun–Thu 08:00–18:00, Fri closed, Sat 08:00–16:00
        for (var day = PlaceDayOfWeek.Sunday; day <= PlaceDayOfWeek.Thursday; day++)
            results.Add(MakeHours(SeedContentIds.BusinessPetraGuides, day, new TimeOnly(8, 0), new TimeOnly(18, 0), false));
        results.Add(MakeHours(SeedContentIds.BusinessPetraGuides, PlaceDayOfWeek.Friday,   TimeOnly.MinValue, TimeOnly.MinValue, true));
        results.Add(MakeHours(SeedContentIds.BusinessPetraGuides, PlaceDayOfWeek.Saturday, new TimeOnly(8, 0), new TimeOnly(16, 0), false));

        // AmmanFood: Sun–Thu 10:00–22:00, Fri 12:00–23:00, Sat 10:00–20:00
        for (var day = PlaceDayOfWeek.Sunday; day <= PlaceDayOfWeek.Thursday; day++)
            results.Add(MakeHours(SeedContentIds.BusinessAmmanFood, day, new TimeOnly(10, 0), new TimeOnly(22, 0), false));
        results.Add(MakeHours(SeedContentIds.BusinessAmmanFood, PlaceDayOfWeek.Friday,   new TimeOnly(12, 0), new TimeOnly(23, 0), false));
        results.Add(MakeHours(SeedContentIds.BusinessAmmanFood, PlaceDayOfWeek.Saturday, new TimeOnly(10, 0), new TimeOnly(20, 0), false));

        // DeadSeaResort: daily 07:00–22:00
        foreach (PlaceDayOfWeek day in Enum.GetValues<PlaceDayOfWeek>())
            results.Add(MakeHours(SeedContentIds.BusinessDeadSeaResort, day, new TimeOnly(7, 0), new TimeOnly(22, 0), false));

        // WadiRumCamp: daily 06:00–20:00
        foreach (PlaceDayOfWeek day in Enum.GetValues<PlaceDayOfWeek>())
            results.Add(MakeHours(SeedContentIds.BusinessWadiRumCamp, day, new TimeOnly(6, 0), new TimeOnly(20, 0), false));

        // AqabaDivers: daily 07:00–18:00
        foreach (PlaceDayOfWeek day in Enum.GetValues<PlaceDayOfWeek>())
            results.Add(MakeHours(SeedContentIds.BusinessAqabaDivers, day, new TimeOnly(7, 0), new TimeOnly(18, 0), false));

        return results;
    }

    private static BusinessHours MakeHours(Guid businessId, PlaceDayOfWeek day, TimeOnly open, TimeOnly close, bool isClosed)
    {
        var row = CreateEntity<BusinessHours>();
        SetProperty(row, nameof(BusinessHours.BusinessId), businessId);
        SetProperty(row, nameof(BusinessHours.DayOfWeek),  day);
        SetProperty(row, nameof(BusinessHours.OpenTime),   open);
        SetProperty(row, nameof(BusinessHours.CloseTime),  close);
        SetProperty(row, nameof(BusinessHours.IsClosed),   isClosed);
        return row;
    }

    private static TEntity CreateEntity<TEntity>()
        where TEntity : class
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
