using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentPlaces.Infrastructure.Persistence.Seeding;

/// <summary>
/// DEV-SEED-B1 — Development / QA-only seeder for the ContentPlaces module.
/// Seeds one verified/featured <see cref="Place"/> (<see cref="DevSeedIds.PlaceId"/>) that the
/// dev tours hang off. Guarded by <see cref="IHostEnvironment.IsDevelopment"/> and idempotent
/// per-row (checks the place id, ignoring the soft-delete filter). Existing seeders untouched.
/// </summary>
public sealed class DevPlacesSeeder(
    ContentPlacesDbContext dbContext,
    IHostEnvironment hostEnvironment,
    ILogger<DevPlacesSeeder> logger) : IModuleDbInitializer
{
    public int Order => 162;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var alreadyExists = await dbContext.Places
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Id == DevSeedIds.PlaceId, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        var place = CreateEntity<Place>();
        SetProperty(place, nameof(Place.Id), DevSeedIds.PlaceId);
        SetProperty(place, nameof(Place.Name), "Dev Seed Heritage Site");
        SetProperty(place, nameof(Place.Slug), "dev-seed-heritage-site");
        SetProperty(place, nameof(Place.Description),
            "DEV-SEED-B1: an approved, verified place used for manual QA of tours and media.");
        SetProperty(place, nameof(Place.PlaceType), PlaceType.Historical);
        SetProperty(place, nameof(Place.Location), new Location(30.3285m, 35.4444m));
        SetProperty(place, nameof(Place.City), "Amman");
        SetProperty(place, nameof(Place.Country), "Jordan");
        SetProperty(place, nameof(Place.AverageRating), 4.7m);
        SetProperty(place, nameof(Place.ReviewCount), 12);
        SetProperty(place, nameof(Place.IsFeatured), true);
        SetProperty(place, nameof(Place.IsVerified), true);
        SetProperty(place, nameof(Place.IsWheelchairAccessible), true);
        SetProperty(place, nameof(Place.HasAudioGuide), true);
        SetProperty(place, nameof(Place.HasBrailleSignage), false);
        SetProperty(place, nameof(Place.CreatedByUserId), DevSeedIds.GuideUserId);

        dbContext.Places.Add(place);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("DEV-SEED-B1: seeded development place {PlaceId}.", DevSeedIds.PlaceId);
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
