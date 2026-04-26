using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentPlaces.Tests.Unit;

internal static class TestBusinessFactory
{
    public static Business CreateBusiness(Guid ownerId)
    {
        return Business.Create(
            name: "Test Business",
            slug: "test-business",
            businessType: BusinessType.Restaurant,
            ownerId: ownerId,
            location: new Location(31.95m, 35.93m));
    }
}
