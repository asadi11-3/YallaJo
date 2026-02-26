using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

public sealed class Review : AuditableEntity, IAggregateRoot
{
    private Review() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid? PlaceId { get; private set; }
    public Guid? TourId { get; private set; }
    public Guid? TourGuideId { get; private set; }
    public Guid? BusinessId { get; private set; }
    public decimal Rating { get; private set; }
    public string? Title { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateOnly? VisitDate { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsReported { get; private set; }
    public int HelpfulCount { get; private set; }

    public static Review CreateForPlace(Guid userId, Guid placeId, decimal rating, string? title, string content, DateOnly? visitDate)
    {
        return new Review
        {
            UserId = userId,
            PlaceId = placeId,
            TourId = null,
            TourGuideId = null,
            BusinessId = null,
            Rating = rating,
            Title = title,
            Content = content,
            VisitDate = visitDate,
            IsVerified = false,
            IsReported = false,
            HelpfulCount = 0
        };
    }

    public static Review CreateForTour(Guid userId, Guid tourId, decimal rating, string? title, string content, DateOnly? visitDate)
    {
        return new Review
        {
            UserId = userId,
            PlaceId = null,
            TourId = tourId,
            TourGuideId = null,
            BusinessId = null,
            Rating = rating,
            Title = title,
            Content = content,
            VisitDate = visitDate,
            IsVerified = false,
            IsReported = false,
            HelpfulCount = 0
        };
    }

    public static Review CreateForTourGuide(Guid userId, Guid tourGuideId, decimal rating, string? title, string content, DateOnly? visitDate)
    {
        return new Review
        {
            UserId = userId,
            PlaceId = null,
            TourId = null,
            TourGuideId = tourGuideId,
            BusinessId = null,
            Rating = rating,
            Title = title,
            Content = content,
            VisitDate = visitDate,
            IsVerified = false,
            IsReported = false,
            HelpfulCount = 0
        };
    }

    public static Review CreateForBusiness(Guid userId, Guid businessId, decimal rating, string? title, string content, DateOnly? visitDate)
    {
        return new Review
        {
            UserId = userId,
            PlaceId = null,
            TourId = null,
            TourGuideId = null,
            BusinessId = businessId,
            Rating = rating,
            Title = title,
            Content = content,
            VisitDate = visitDate,
            IsVerified = false,
            IsReported = false,
            HelpfulCount = 0
        };
    }
}
