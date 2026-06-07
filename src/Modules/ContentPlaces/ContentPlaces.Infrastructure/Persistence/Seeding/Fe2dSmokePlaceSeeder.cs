using System.Reflection;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentPlaces.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only FE-2D smoke seeder for PLACE UI states.
///
/// Seeds clearly-labelled ("FE2D Smoke Place …") deterministic Place rows so a tester can visually
/// verify the public place list / detail UI in every important state:
///   • Full place      — description, coordinates, featured+verified, accessibility features.
///   • Minimal place    — required fields + description, no optional sections / no images / no reviews.
///   • Empty place      — only required fields, exercises every empty-state on the detail page.
///   • Hidden place     — soft-deleted (IsDeleted=true) to confirm non-public rows never appear publicly.
///   • List/search rows — a couple of extra visible places (distinct cities) for list/search/pagination.
///
/// Safety / conventions:
///   • Invoked only from <c>UseDataSeedingAsync</c> (Development-gated; or explicit <c>Seeding:Enabled</c>).
///   • No production data, no real PII (fictional "FE2D" venues only).
///   • Idempotent: per-slug guard via <c>IgnoreQueryFilters()</c> (so the soft-deleted row is not re-added).
///   • Reflection construction (matching <see cref="ContentPlacesDbInitializer"/>) bypasses the
///     <see cref="Place.Create"/> factory so deterministic GUIDs / CreatedAt / soft-delete can be set
///     and no domain events are raised.
///   • Respects unique indexes: Place.Slug (global) and (Name, Country) filtered-unique.
///   • Order 64 — after <see cref="ContentPlacesDbInitializer"/> (60); whether or not it ran, this
///     seeder is self-contained (does not depend on the base places existing).
///
/// Backend limitations (documented, NOT worked around here):
///   • Places have NO status/approval enum — visibility is purely <c>!IsDeleted</c>. "pending /
///     rejected / suspended" place statuses do not exist; the closest non-public state is soft-delete
///     (seeded as "FE2D Smoke Place Hidden").
///   • Place images live in the ContentCore Attachments module, not ContentPlaces, so this seeder
///     cannot attach images (the "no images" empty state is therefore the default for all seeded places).
///   • Category GUIDs are created non-deterministically by ContentCore, so CategoryId is left null
///     (matching the existing Petra/Jerash seeds).
/// </summary>
public sealed class Fe2dSmokePlaceSeeder(ContentPlacesDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid CreatedBy = Guid.Parse("11111111-1111-1111-1111-111111111111"); // owner@yallajo.local

    // Deterministic place ids (fe2d… prefix keeps them obvious and collision-free).
    public static readonly Guid PlaceFullId    = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b1");
    public static readonly Guid PlaceMinimalId = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b2");
    public static readonly Guid PlaceEmptyId   = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b3");
    public static readonly Guid PlaceHiddenId  = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b4");
    public static readonly Guid PlaceListAId   = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b5");
    public static readonly Guid PlaceListBId   = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b6");

    public const string SlugFull    = "fe2d-smoke-place-full";
    public const string SlugMinimal = "fe2d-smoke-place-minimal";
    public const string SlugEmpty   = "fe2d-smoke-place-empty";
    public const string SlugHidden  = "fe2d-smoke-place-hidden";
    public const string SlugListA   = "fe2d-smoke-place-list-a";
    public const string SlugListB   = "fe2d-smoke-place-list-b";

    public int Order => 64;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var existingSlugs = new HashSet<string>(
            await dbContext.Places
                .IgnoreQueryFilters() // include soft-deleted so the Hidden row is not re-inserted
                .Where(p => p.Slug.StartsWith("fe2d-smoke-place-"))
                .Select(p => p.Slug)
                .ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var places = new List<Place>();
        var features = new List<AccessibilityFeature>();

        // ── 1. FULL place ───────────────────────────────────────────────────
        if (existingSlugs.Add(SlugFull))
        {
            places.Add(BuildPlace(
                id: PlaceFullId, name: "FE2D Smoke Place Full", slug: SlugFull,
                placeType: PlaceType.Historical, lat: 31.9632m, lng: 35.9306m,
                description: "FE2D smoke-test place with full content: description, coordinates, city/country, " +
                             "contact details, accessibility features, featured + verified badges, plus reviews and " +
                             "FE-2D accessibility reviews. Development data only.",
                address: "1 Citadel Hill", city: "FE2D City", country: "FE2D Land",
                phone: "+10000000001", email: "full@fe2d.local", website: "https://fe2d.local/full",
                isFeatured: true, isVerified: true,
                wheelchair: true, audioGuide: true, braille: true, createdAt: now));

            // Place-level accessibility features (served at GET /places/{id}/accessibility).
            features.Add(AccessibilityFeature.Create(AccessibilityFeature.EntityTypePlace, PlaceFullId,
                AccessibilityFeatureType.Wheelchair, "Step-free entrance", "Ramp access at the main gate.", true));
            features.Add(AccessibilityFeature.Create(AccessibilityFeature.EntityTypePlace, PlaceFullId,
                AccessibilityFeatureType.Visual, "Braille signage", "Braille on all directional signage.", true));
            features.Add(AccessibilityFeature.Create(AccessibilityFeature.EntityTypePlace, PlaceFullId,
                AccessibilityFeatureType.Hearing, "Audio guide", "Audio guide with hearing-loop support.", true));
            features.Add(AccessibilityFeature.Create(AccessibilityFeature.EntityTypePlace, PlaceFullId,
                AccessibilityFeatureType.Mobility, "Accessible restroom", null, true));
        }

        // ── 2. MINIMAL place (required fields + a short description) ──────────
        if (existingSlugs.Add(SlugMinimal))
        {
            places.Add(BuildPlace(
                id: PlaceMinimalId, name: "FE2D Smoke Place Minimal", slug: SlugMinimal,
                placeType: PlaceType.Attraction, lat: 32.5556m, lng: 35.8500m,
                description: "FE2D minimal visible place: required fields only, no images, no reviews, no " +
                             "accessibility features. Used to verify the detail page's empty sections.",
                address: null, city: "FE2D Town", country: "FE2D Land",
                phone: null, email: null, website: null,
                isFeatured: false, isVerified: true,
                wheelchair: false, audioGuide: false, braille: false, createdAt: now));
        }

        // ── 3. EMPTY place (only required fields — strongest empty-state) ─────
        if (existingSlugs.Add(SlugEmpty))
        {
            places.Add(BuildPlace(
                id: PlaceEmptyId, name: "FE2D Smoke Place Empty", slug: SlugEmpty,
                placeType: PlaceType.Nature, lat: 30.5852m, lng: 35.4732m,
                description: null,
                address: null, city: null, country: null,
                phone: null, email: null, website: null,
                isFeatured: false, isVerified: false,
                wheelchair: false, audioGuide: false, braille: false, createdAt: now));
        }

        // ── 4. HIDDEN place (soft-deleted — must NOT appear publicly) ─────────
        if (existingSlugs.Add(SlugHidden))
        {
            var hidden = BuildPlace(
                id: PlaceHiddenId, name: "FE2D Smoke Place Hidden", slug: SlugHidden,
                placeType: PlaceType.Entertainment, lat: 29.5320m, lng: 35.0063m,
                description: "FE2D soft-deleted place. Documents the only non-public Place state (Places have no " +
                             "status enum). Should never appear in the public list or detail page.",
                address: null, city: "FE2D Hidden City", country: "FE2D Land",
                phone: null, email: null, website: null,
                isFeatured: false, isVerified: false,
                wheelchair: false, audioGuide: false, braille: false, createdAt: now);
            SetProperty(hidden, nameof(Place.IsDeleted), true);
            SetProperty(hidden, nameof(Place.DeletedAt), now.AddDays(-1));
            places.Add(hidden);
        }

        // ── 5 & 6. Extra visible rows for list / search / pagination ─────────
        if (existingSlugs.Add(SlugListA))
        {
            places.Add(BuildPlace(
                id: PlaceListAId, name: "FE2D Smoke Place List A", slug: SlugListA,
                placeType: PlaceType.Restaurant, lat: 31.2000m, lng: 35.7000m,
                description: "FE2D additional visible place (City Aqaba) for list/search/pagination testing.",
                address: null, city: "Aqaba", country: "FE2D Land",
                phone: null, email: null, website: null,
                isFeatured: true, isVerified: true,
                wheelchair: false, audioGuide: false, braille: false, createdAt: now));
        }

        if (existingSlugs.Add(SlugListB))
        {
            places.Add(BuildPlace(
                id: PlaceListBId, name: "FE2D Smoke Place List B", slug: SlugListB,
                placeType: PlaceType.Shopping, lat: 32.0833m, lng: 36.0933m,
                description: "FE2D additional visible place (City Zarqa) for list/search/pagination testing.",
                address: null, city: "Zarqa", country: "FE2D Land",
                phone: null, email: null, website: null,
                isFeatured: false, isVerified: true,
                wheelchair: false, audioGuide: false, braille: false, createdAt: now));
        }

        if (places.Count == 0)
            return;

        // Strip any domain events so seeding has no outbox side-effects.
        foreach (var p in places)
            p.ClearDomainEvents();

        dbContext.Places.AddRange(places);
        if (features.Count > 0)
            dbContext.AccessibilityFeatures.AddRange(features);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Place BuildPlace(
        Guid id, string name, string slug, PlaceType placeType, decimal lat, decimal lng,
        string? description, string? address, string? city, string? country,
        string? phone, string? email, string? website,
        bool isFeatured, bool isVerified, bool wheelchair, bool audioGuide, bool braille,
        DateTime createdAt)
    {
        var place = CreateEntity<Place>();
        SetProperty(place, nameof(Place.Id), id);
        SetProperty(place, nameof(Place.Name), name);
        SetProperty(place, nameof(Place.Slug), slug);
        SetProperty(place, nameof(Place.PlaceType), placeType);
        SetProperty(place, nameof(Place.Location), new Location(lat, lng));
        SetProperty(place, nameof(Place.Description), description);
        SetProperty(place, nameof(Place.Address), address);
        SetProperty(place, nameof(Place.City), city);
        SetProperty(place, nameof(Place.Country), country);
        SetProperty(place, nameof(Place.Phone), phone);
        SetProperty(place, nameof(Place.Email), email);
        SetProperty(place, nameof(Place.Website), website);
        SetProperty(place, nameof(Place.IsFeatured), isFeatured);
        SetProperty(place, nameof(Place.IsVerified), isVerified);
        SetProperty(place, nameof(Place.IsWheelchairAccessible), wheelchair);
        SetProperty(place, nameof(Place.HasAudioGuide), audioGuide);
        SetProperty(place, nameof(Place.HasBrailleSignage), braille);
        SetProperty(place, nameof(Place.CreatedByUserId), CreatedBy);
        SetProperty(place, nameof(Place.CreatedAt), createdAt);
        return place;
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
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property is null)
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");

        property.SetValue(target, value);
    }
}
