using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.ValueObjects;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Analytics.Infrastructure.Persistence.Seeding;

public sealed class AnalyticsDbInitializer(AnalyticsDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid TravelerOne = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid PreferenceId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000001");
    private static readonly Guid PopularityId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000002");
    private static readonly Guid RecommendationId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000003");

    public int Order => 130;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.PopularityScores.AnyAsync(cancellationToken))
        {
            return;
        }

        var auditLogs = CreateAuditLogs();
        var interactions = CreateInteractions();
        var preferences = CreatePreferences();
        var preferredCategories = CreatePreferredCategories();
        var popularityScores = CreatePopularityScores();
        var recommendations = CreateRecommendations();

        dbContext.AuditLogs.AddRange(auditLogs);
        dbContext.UserInteractions.AddRange(interactions);
        dbContext.UserPreferences.AddRange(preferences);
        dbContext.UserPreferredCategories.AddRange(preferredCategories);
        dbContext.PopularityScores.AddRange(popularityScores);
        dbContext.RecommendationCaches.AddRange(recommendations);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<AuditLog> CreateAuditLogs()
    {
        var first = CreateEntity<AuditLog>();
        SetProperty(first, nameof(AuditLog.UserId), TravelerOne);
        SetProperty(first, nameof(AuditLog.UserAgent), "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        SetProperty(first, nameof(AuditLog.Action), "TourBookingCreated");
        SetProperty(first, nameof(AuditLog.EntityType), "TourBooking");
        SetProperty(first, nameof(AuditLog.EntityId), SeedBookingIds.BookingOne.ToString());
        SetProperty(first, nameof(AuditLog.IpAddress), "10.0.1.21");
        SetProperty(first, nameof(AuditLog.NewValues), "{\"status\":\"Confirmed\"}");
        SetProperty(first, nameof(AuditLog.OccurredAt), DateTime.UtcNow.AddHours(-12));

        return [first];
    }

    private static List<UserInteraction> CreateInteractions()
    {
        var view = CreateEntity<UserInteraction>();
        SetProperty(view, nameof(UserInteraction.UserId), TravelerOne);
        SetProperty(view, nameof(UserInteraction.InteractionType), InteractionType.View);
        SetProperty(view, nameof(UserInteraction.EntityType), "Tour");
        SetProperty(view, nameof(UserInteraction.EntityId), SeedContentIds.TourPetraExplorer);
        SetProperty(view, nameof(UserInteraction.Location), new Location(30.3285m, 35.4444m));
        SetProperty(view, nameof(UserInteraction.SessionId), "analytics-ses-001");
        SetProperty(view, nameof(UserInteraction.DeviceType), "Web");
        SetProperty(view, nameof(UserInteraction.DurationSeconds), 95);
        SetProperty(view, nameof(UserInteraction.OccurredAt), DateTime.UtcNow.AddMinutes(-80));

        var booking = CreateEntity<UserInteraction>();
        SetProperty(booking, nameof(UserInteraction.UserId), TravelerTwo);
        SetProperty(booking, nameof(UserInteraction.InteractionType), InteractionType.Booking);
        SetProperty(booking, nameof(UserInteraction.EntityType), "TourBooking");
        SetProperty(booking, nameof(UserInteraction.EntityId), SeedBookingIds.BookingTwo);
        SetProperty(booking, nameof(UserInteraction.Location), new Location(30.3220m, 35.4780m));
        SetProperty(booking, nameof(UserInteraction.SessionId), "analytics-ses-002");
        SetProperty(booking, nameof(UserInteraction.DeviceType), "Android");
        SetProperty(booking, nameof(UserInteraction.DurationSeconds), 140);
        SetProperty(booking, nameof(UserInteraction.OccurredAt), DateTime.UtcNow.AddMinutes(-35));

        return [view, booking];
    }

    private static List<UserPreference> CreatePreferences()
    {
        var preference = CreateEntity<UserPreference>();
        SetProperty(preference, nameof(UserPreference.Id), PreferenceId);
        SetProperty(preference, nameof(UserPreference.UserId), TravelerOne);
        SetProperty(preference, nameof(UserPreference.PreferenceKey), "preferred_language");
        SetProperty(preference, nameof(UserPreference.PreferenceValue), "en");
        return [preference];
    }

    private static List<UserPreferredCategory> CreatePreferredCategories()
    {
        var preferred = CreateEntity<UserPreferredCategory>();
        SetProperty(preferred, nameof(UserPreferredCategory.UserId), TravelerOne);
        SetProperty(preferred, nameof(UserPreferredCategory.CategoryId), Guid.Parse("10101010-1010-1010-1010-101010101010"));
        SetProperty(preferred, nameof(UserPreferredCategory.PreferenceScore), 0.91m);
        return [preferred];
    }

    private static List<PopularityScore> CreatePopularityScores()
    {
        var score = CreateEntity<PopularityScore>();
        SetProperty(score, nameof(PopularityScore.Id), PopularityId);
        SetProperty(score, nameof(PopularityScore.EntityType), "Tour");
        SetProperty(score, nameof(PopularityScore.EntityId), SeedContentIds.TourPetraExplorer);
        SetProperty(score, nameof(PopularityScore.TrendingScore), 87.3200m);
        SetProperty(score, nameof(PopularityScore.ViewCount), 1260);
        SetProperty(score, nameof(PopularityScore.BookmarkCount), 145);
        SetProperty(score, nameof(PopularityScore.ShareCount), 63);
        SetProperty(score, nameof(PopularityScore.BookingCount), 412);
        SetProperty(score, nameof(PopularityScore.ReviewScore), 4.70m);
        SetProperty(score, nameof(PopularityScore.LastCalculatedAt), DateTime.UtcNow.AddMinutes(-15));
        return [score];
    }

    private static List<RecommendationCache> CreateRecommendations()
    {
        var recommendation = CreateEntity<RecommendationCache>();
        SetProperty(recommendation, nameof(RecommendationCache.Id), RecommendationId);
        SetProperty(recommendation, nameof(RecommendationCache.UserId), TravelerTwo);
        SetProperty(recommendation, nameof(RecommendationCache.EntityType), "Tour");
        SetProperty(recommendation, nameof(RecommendationCache.EntityId), SeedContentIds.TourPetraExplorer);
        SetProperty(recommendation, nameof(RecommendationCache.Score), 0.8842m);
        SetProperty(recommendation, nameof(RecommendationCache.Reason), "Similar travelers booked this Petra route after viewing historical categories.");
        SetProperty(recommendation, nameof(RecommendationCache.GeneratedAt), DateTime.UtcNow.AddMinutes(-5));
        SetProperty(recommendation, nameof(RecommendationCache.ExpiresAt), DateTime.UtcNow.AddHours(8));
        return [recommendation];
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
