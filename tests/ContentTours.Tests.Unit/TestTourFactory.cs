using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Tests.Unit;

internal static class TestTourFactory
{
    public static Tour CreateDraft(
        Guid? createdByUserId = null,
        string? slug = null,
        Guid? placeId = null,
        Location? meetingPoint = null,
        string? description = null)
    {
        return Tour.Create(
            name:                    "Petra Day Tour",
            slug:                    slug ?? $"petra-{Guid.NewGuid():N}",
            difficulty:              Difficulty.Easy,
            durationMinutes:         480,
            maxGroupSize:            20,
            basePriceAmount:         100m,
            currency:                "JOD",
            location:                new Location(30.32m, 35.45m),
            createdByUserId:         createdByUserId ?? Guid.NewGuid(),
            description:             description,
            shortDescription:        null,
            minAge:                  null,
            meetingPoint:            meetingPoint,
            placeId:                 placeId ?? Guid.NewGuid());
    }

    public static Tour CreatePending(Guid? createdByUserId = null)
    {
        var tour = CreateDraft(
            createdByUserId: createdByUserId,
            meetingPoint:    new Location(30.32m, 35.45m),
            description:     new string('a', 120));
        tour.ClearDomainEvents();
        tour.Submit();
        return tour;
    }

    public static Tour CreateApproved(Guid? createdByUserId = null, Guid? approvedBy = null)
    {
        var tour = CreatePending(createdByUserId);
        tour.ClearDomainEvents();
        tour.Approve(approvedBy ?? Guid.NewGuid());
        return tour;
    }

    public static Tour CreateRejected(Guid? createdByUserId = null, Guid? rejectedBy = null, string reason = "Insufficient detail.")
    {
        var tour = CreatePending(createdByUserId);
        tour.ClearDomainEvents();
        tour.Reject(reason, rejectedBy ?? Guid.NewGuid());
        return tour;
    }

    public static Tour CreateSuspended(Guid? createdByUserId = null, string reason = "Policy violation.")
    {
        var tour = CreateApproved(createdByUserId);
        tour.ClearDomainEvents();
        tour.Suspend(reason);
        return tour;
    }
}
