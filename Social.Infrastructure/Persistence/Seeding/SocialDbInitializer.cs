using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Social.Infrastructure.Persistence.Seeding;

public sealed class SocialDbInitializer(SocialDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid TravelerOne = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid TravelerThree = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid TravelerFour = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid TravelerFive = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PlacePetra = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid PlaceJerash = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid BusinessPetraGuides = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    public int Order => 70;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Reviews.AnyAsync(cancellationToken))
        {
            return;
        }

        var reviews = CreateReviews();
        var favorites = CreateFavorites();
        var reports = CreateReports();
        var accessibilityReviews = CreateAccessibilityReviews();

        dbContext.Reviews.AddRange(reviews);
        dbContext.Favorites.AddRange(favorites);
        dbContext.Reports.AddRange(reports);
        dbContext.AccessibilityReviews.AddRange(accessibilityReviews);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Review> CreateReviews()
    {
        return
        [
            Review.CreateForPlace(
                TravelerOne,
                PlacePetra,
                4.8m,
                "Amazing history",
                "The guide was excellent and the route was well organized.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20))),
            Review.CreateForBusiness(
                TravelerTwo,
                BusinessPetraGuides,
                4.6m,
                "Professional team",
                "Very punctual and knowledgeable local guide.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14))),
            Review.CreateForPlace(
                TravelerThree,
                PlaceJerash,
                4.4m,
                "Great destination",
                "Beautiful ruins and easy to navigate.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-9)))
        ];
    }

    private static List<Favorite> CreateFavorites()
    {
        var favorite1 = CreateEntity<Favorite>();
        SetProperty(favorite1, nameof(Favorite.UserId), TravelerOne);
        SetProperty(favorite1, nameof(Favorite.EntityType), "Place");
        SetProperty(favorite1, nameof(Favorite.EntityId), PlacePetra);

        var favorite2 = CreateEntity<Favorite>();
        SetProperty(favorite2, nameof(Favorite.UserId), TravelerTwo);
        SetProperty(favorite2, nameof(Favorite.EntityType), "Business");
        SetProperty(favorite2, nameof(Favorite.EntityId), BusinessPetraGuides);

        return [favorite1, favorite2];
    }

    private static List<Report> CreateReports()
    {
        var report = CreateEntity<Report>();
        SetProperty(report, nameof(Report.ReporterUserId), TravelerFour);
        SetProperty(report, nameof(Report.EntityType), "Review");
        SetProperty(report, nameof(Report.EntityId), Guid.Parse("dddddddd-0000-0000-0000-000000000001"));
        SetProperty(report, nameof(Report.Reason), ReportReason.Other);
        SetProperty(report, nameof(Report.Description), "Potentially inaccurate details in this review.");
        SetProperty(report, nameof(Report.Status), ReportStatus.Pending);

        return [report];
    }

    private static List<AccessibilityReview> CreateAccessibilityReviews()
    {
        var review = CreateEntity<AccessibilityReview>();
        SetProperty(review, nameof(AccessibilityReview.UserId), TravelerFive);
        SetProperty(review, nameof(AccessibilityReview.EntityType), "Place");
        SetProperty(review, nameof(AccessibilityReview.EntityId), PlaceJerash);
        SetProperty(review, nameof(AccessibilityReview.WheelchairAccessible), true);
        SetProperty(review, nameof(AccessibilityReview.VisualAidAvailable), false);
        SetProperty(review, nameof(AccessibilityReview.HearingAidAvailable), false);
        SetProperty(review, nameof(AccessibilityReview.AccessibilityRating), 3.8m);
        SetProperty(review, nameof(AccessibilityReview.Comments), "Main paths are accessible but some sections are uneven.");
        SetProperty(review, nameof(AccessibilityReview.VisitDate), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12)));

        return [review];
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
