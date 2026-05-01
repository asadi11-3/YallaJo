using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Tests.Unit.Mohammad;

internal static class TestPricingTierFactory
{
    public static TourPricingTier Create(
        Guid tourId,
        string name = "Adult",
        ParticipantType participantType = ParticipantType.Adult,
        decimal price = 50m,
        string currency = "JOD",
        int min = 1,
        int? max = null,
        bool isActive = true)
    {
        var tier = TourPricingTier.Create(
            tourId:          tourId,
            name:            name,
            description:     null,
            price:           new Money(price, currency),
            participantType: participantType,
            minParticipants: min,
            maxParticipants: max);
        if (!isActive) tier.Deactivate();
        return tier;
    }
}
