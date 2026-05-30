using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Social.Infrastructure.Persistence.Seeding;

public sealed class SocialDbInitializer(SocialDbContext dbContext) : IModuleDbInitializer
{
    private static readonly Guid TravelerOne   = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo   = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid TravelerThree = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid TravelerFour  = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid PlacePetra    = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid PlaceJerash   = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid BusinessPetraGuides = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    public int Order => 70;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Reviews.AnyAsync(cancellationToken))
            return;

        dbContext.Reviews.AddRange(CreateReviews());
        dbContext.Favorites.AddRange(CreateFavorites());
        dbContext.Reports.AddRange(CreateReports());

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<Review> CreateReviews()
    {
        return
        [
            CreateReview(TravelerOne,   ReviewTargetType.Place,    PlacePetra,         4.8m,
                "Amazing history",    "The guide was excellent and the route was well organized.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20))),
            CreateReview(TravelerTwo,   ReviewTargetType.Business, BusinessPetraGuides, 4.6m,
                "Professional team",  "Very punctual and knowledgeable local guide.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14))),
            CreateReview(TravelerThree, ReviewTargetType.Place,    PlaceJerash,         4.4m,
                "Great destination",  "Beautiful ruins and easy to navigate.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-9)))
        ];
    }

    private static Review CreateReview(
        Guid userId, ReviewTargetType targetType, Guid targetId,
        decimal rating, string title, string content, DateOnly? visitDate)
    {
        var review = CreateEntity<Review>();
        SetProperty(review, nameof(Review.UserId), userId);
        SetProperty(review, nameof(Review.TargetType), targetType);
        SetProperty(review, nameof(Review.TargetId), targetId);
        SetProperty(review, nameof(Review.Rating), rating);
        SetProperty(review, nameof(Review.Title), title);
        SetProperty(review, nameof(Review.Content), content);
        SetProperty(review, nameof(Review.VisitDate), visitDate);
        SetProperty(review, nameof(Review.Status), ReviewStatus.Published);
        SetProperty(review, nameof(Review.IsVerifiedBooking), false);
        SetProperty(review, nameof(Review.ProfanityFlagged), false);
        SetProperty(review, nameof(Review.CurrentReportCount), 0);
        return review;
    }

    private static List<Favorite> CreateFavorites()
    {
        var f1 = CreateEntity<Favorite>();
        SetProperty(f1, nameof(Favorite.UserId), TravelerOne);
        SetProperty(f1, nameof(Favorite.EntityType), FavoriteEntityType.Place);
        SetProperty(f1, nameof(Favorite.EntityId), PlacePetra);
        SetProperty(f1, nameof(Favorite.AddedAt), DateTime.UtcNow);

        var f2 = CreateEntity<Favorite>();
        SetProperty(f2, nameof(Favorite.UserId), TravelerTwo);
        SetProperty(f2, nameof(Favorite.EntityType), FavoriteEntityType.Business);
        SetProperty(f2, nameof(Favorite.EntityId), BusinessPetraGuides);
        SetProperty(f2, nameof(Favorite.AddedAt), DateTime.UtcNow);

        return [f1, f2];
    }

    private static List<Report> CreateReports()
    {
        var report = CreateEntity<Report>();
        SetProperty(report, nameof(Report.ReporterUserId), TravelerFour);
        SetProperty(report, nameof(Report.EntityType), ReportableEntityType.Review);
        SetProperty(report, nameof(Report.EntityId), Guid.Parse("dddddddd-0000-0000-0000-000000000001"));
        SetProperty(report, nameof(Report.Reason), ReportReason.Other);
        SetProperty(report, nameof(Report.Description), "Potentially inaccurate details in this review.");
        SetProperty(report, nameof(Report.Status), ReportStatus.Open);
        SetProperty(report, nameof(Report.SubmittedAt), DateTime.UtcNow);
        return [report];
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
