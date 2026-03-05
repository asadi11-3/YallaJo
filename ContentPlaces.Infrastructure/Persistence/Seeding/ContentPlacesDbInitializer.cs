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

        var places = CreatePlaces();
        var businesses = CreateBusinesses();
        var placeBusinesses = CreatePlaceBusinesses();
        var amenities = CreateAmenities();
        var hours = CreateBusinessHours();

        dbContext.Places.AddRange(places);
        dbContext.Businesses.AddRange(businesses);
        dbContext.PlaceBusinesses.AddRange(placeBusinesses);
        dbContext.BusinessAmenities.AddRange(amenities);
        dbContext.BusinessHours.AddRange(hours);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Place> CreatePlaces()
    {
        var petra = CreateEntity<Place>();
        SetProperty(petra, nameof(Place.Id), SeedContentIds.PlacePetra);
        SetProperty(petra, nameof(Place.Name), "Petra Archaeological Park");
        SetProperty(petra, nameof(Place.Slug), "petra-archaeological-park");
        SetProperty(petra, nameof(Place.Description), "Historic Nabatean city and UNESCO World Heritage site.");
        SetProperty(petra, nameof(Place.PlaceType), PlaceType.Historical);
        SetProperty(petra, nameof(Place.Location), new Location(30.3285m, 35.4444m));
        SetProperty(petra, nameof(Place.City), "Petra");
        SetProperty(petra, nameof(Place.Country), "Jordan");
        SetProperty(petra, nameof(Place.AverageRating), 4.8m);
        SetProperty(petra, nameof(Place.ReviewCount), 1240);
        SetProperty(petra, nameof(Place.IsFeatured), true);
        SetProperty(petra, nameof(Place.IsVerified), true);
        SetProperty(petra, nameof(Place.IsWheelchairAccessible), false);
        SetProperty(petra, nameof(Place.HasAudioGuide), true);
        SetProperty(petra, nameof(Place.HasBrailleSignage), false);
        SetProperty(petra, nameof(Place.CreatedByUserId), SeedIdentityProfiles.All[0].UserId);

        var jerash = CreateEntity<Place>();
        SetProperty(jerash, nameof(Place.Id), SeedContentIds.PlaceJerash);
        SetProperty(jerash, nameof(Place.Name), "Jerash Roman Ruins");
        SetProperty(jerash, nameof(Place.Slug), "jerash-roman-ruins");
        SetProperty(jerash, nameof(Place.Description), "One of the largest and best-preserved Roman sites outside Italy.");
        SetProperty(jerash, nameof(Place.PlaceType), PlaceType.Historical);
        SetProperty(jerash, nameof(Place.Location), new Location(32.2744m, 35.8914m));
        SetProperty(jerash, nameof(Place.City), "Jerash");
        SetProperty(jerash, nameof(Place.Country), "Jordan");
        SetProperty(jerash, nameof(Place.AverageRating), 4.6m);
        SetProperty(jerash, nameof(Place.ReviewCount), 810);
        SetProperty(jerash, nameof(Place.IsFeatured), false);
        SetProperty(jerash, nameof(Place.IsVerified), true);
        SetProperty(jerash, nameof(Place.IsWheelchairAccessible), true);
        SetProperty(jerash, nameof(Place.HasAudioGuide), true);
        SetProperty(jerash, nameof(Place.HasBrailleSignage), false);
        SetProperty(jerash, nameof(Place.CreatedByUserId), SeedIdentityProfiles.All[0].UserId);

        return [petra, jerash];
    }

    private static List<Business> CreateBusinesses()
    {
        var petraGuides = CreateEntity<Business>();
        SetProperty(petraGuides, nameof(Business.Id), SeedContentIds.BusinessPetraGuides);
        SetProperty(petraGuides, nameof(Business.Name), "Petra Local Guides");
        SetProperty(petraGuides, nameof(Business.Slug), "petra-local-guides");
        SetProperty(petraGuides, nameof(Business.Description), "Licensed local guides for Petra and Wadi Musa experiences.");
        SetProperty(petraGuides, nameof(Business.BusinessType), BusinessType.Guide);
        SetProperty(petraGuides, nameof(Business.PlaceId), SeedContentIds.PlacePetra);
        SetProperty(petraGuides, nameof(Business.Location), new Location(30.3220m, 35.4780m));
        SetProperty(petraGuides, nameof(Business.City), "Wadi Musa");
        SetProperty(petraGuides, nameof(Business.Country), "Jordan");
        SetProperty(petraGuides, nameof(Business.Email), "contact@petralocalguides.jo");
        SetProperty(petraGuides, nameof(Business.Phone), "+962790000001");
        SetProperty(petraGuides, nameof(Business.AverageRating), 4.7m);
        SetProperty(petraGuides, nameof(Business.ReviewCount), 320);
        SetProperty(petraGuides, nameof(Business.IsVerified), true);
        SetProperty(petraGuides, nameof(Business.IsFeatured), true);
        SetProperty(petraGuides, nameof(Business.OwnerId), SeedIdentityProfiles.All[3].UserId);
        SetProperty(petraGuides, nameof(Business.Status), BusinessStatus.Approved);

        var ammanFood = CreateEntity<Business>();
        SetProperty(ammanFood, nameof(Business.Id), SeedContentIds.BusinessAmmanFood);
        SetProperty(ammanFood, nameof(Business.Name), "Amman Food Walks");
        SetProperty(ammanFood, nameof(Business.Slug), "amman-food-walks");
        SetProperty(ammanFood, nameof(Business.Description), "Street-food and local cuisine guided walks in downtown Amman.");
        SetProperty(ammanFood, nameof(Business.BusinessType), BusinessType.Agency);
        SetProperty(ammanFood, nameof(Business.PlaceId), SeedContentIds.PlaceJerash);
        SetProperty(ammanFood, nameof(Business.Location), new Location(31.9516m, 35.9239m));
        SetProperty(ammanFood, nameof(Business.City), "Amman");
        SetProperty(ammanFood, nameof(Business.Country), "Jordan");
        SetProperty(ammanFood, nameof(Business.Email), "hello@ammanfoodwalks.jo");
        SetProperty(ammanFood, nameof(Business.Phone), "+962790000002");
        SetProperty(ammanFood, nameof(Business.AverageRating), 4.5m);
        SetProperty(ammanFood, nameof(Business.ReviewCount), 210);
        SetProperty(ammanFood, nameof(Business.IsVerified), true);
        SetProperty(ammanFood, nameof(Business.IsFeatured), false);
        SetProperty(ammanFood, nameof(Business.OwnerId), SeedIdentityProfiles.All[4].UserId);
        SetProperty(ammanFood, nameof(Business.Status), BusinessStatus.Approved);

        return [petraGuides, ammanFood];
    }

    private static List<PlaceBusiness> CreatePlaceBusinesses()
    {
        var first = CreateEntity<PlaceBusiness>();
        SetProperty(first, nameof(PlaceBusiness.PlaceId), SeedContentIds.PlacePetra);
        SetProperty(first, nameof(PlaceBusiness.BusinessId), SeedContentIds.BusinessPetraGuides);

        var second = CreateEntity<PlaceBusiness>();
        SetProperty(second, nameof(PlaceBusiness.PlaceId), SeedContentIds.PlaceJerash);
        SetProperty(second, nameof(PlaceBusiness.BusinessId), SeedContentIds.BusinessAmmanFood);

        return [first, second];
    }

    private static List<BusinessAmenity> CreateAmenities()
    {
        var wifi = CreateEntity<BusinessAmenity>();
        SetProperty(wifi, nameof(BusinessAmenity.BusinessId), SeedContentIds.BusinessPetraGuides);
        SetProperty(wifi, nameof(BusinessAmenity.Name), "Wifi");
        SetProperty(wifi, nameof(BusinessAmenity.Icon), "wifi");
        SetProperty(wifi, nameof(BusinessAmenity.SortOrder), 1);

        var parking = CreateEntity<BusinessAmenity>();
        SetProperty(parking, nameof(BusinessAmenity.BusinessId), SeedContentIds.BusinessAmmanFood);
        SetProperty(parking, nameof(BusinessAmenity.Name), "Parking");
        SetProperty(parking, nameof(BusinessAmenity.Icon), "parking");
        SetProperty(parking, nameof(BusinessAmenity.SortOrder), 1);

        return [wifi, parking];
    }

    private static List<BusinessHours> CreateBusinessHours()
    {
        var results = new List<BusinessHours>();
        for (var day = PlaceDayOfWeek.Sunday; day <= PlaceDayOfWeek.Thursday; day++)
        {
            results.Add(CreateBusinessHoursRow(SeedContentIds.BusinessPetraGuides, day, new TimeOnly(8, 0), new TimeOnly(18, 0), isClosed: false));
            results.Add(CreateBusinessHoursRow(SeedContentIds.BusinessAmmanFood, day, new TimeOnly(10, 0), new TimeOnly(22, 0), isClosed: false));
        }

        results.Add(CreateBusinessHoursRow(SeedContentIds.BusinessPetraGuides, PlaceDayOfWeek.Friday, new TimeOnly(0, 0), new TimeOnly(0, 0), isClosed: true));
        results.Add(CreateBusinessHoursRow(SeedContentIds.BusinessAmmanFood, PlaceDayOfWeek.Friday, new TimeOnly(12, 0), new TimeOnly(23, 0), isClosed: false));
        results.Add(CreateBusinessHoursRow(SeedContentIds.BusinessPetraGuides, PlaceDayOfWeek.Saturday, new TimeOnly(8, 0), new TimeOnly(16, 0), isClosed: false));
        results.Add(CreateBusinessHoursRow(SeedContentIds.BusinessAmmanFood, PlaceDayOfWeek.Saturday, new TimeOnly(10, 0), new TimeOnly(20, 0), isClosed: false));

        return results;
    }

    private static BusinessHours CreateBusinessHoursRow(Guid businessId, PlaceDayOfWeek day, TimeOnly open, TimeOnly close, bool isClosed)
    {
        var row = CreateEntity<BusinessHours>();
        SetProperty(row, nameof(BusinessHours.BusinessId), businessId);
        SetProperty(row, nameof(BusinessHours.DayOfWeek), day);
        SetProperty(row, nameof(BusinessHours.OpenTime), open);
        SetProperty(row, nameof(BusinessHours.CloseTime), close);
        SetProperty(row, nameof(BusinessHours.IsClosed), isClosed);
        return row;
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
